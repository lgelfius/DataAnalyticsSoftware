using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using Excel = Microsoft.Office.Interop.Excel;
using System.Net;
using Microsoft.VisualBasic.FileIO;
using OxyPlot;
using OxyPlot.Series;
using OfficeOpenXml;
using MathNet.Numerics;

namespace Pants_Unzipper
{
    /// <summary>
    /// Main window - hosts all main processing functions
    /// Luke Gelfius
    /// EE - Senior Design 2018-2019
    /// </summary>
    /// 

    //Some important notes:
    //Data that gets processed in this program comes in a manor similar to this:
    // TIMESTAMP    HEX NAME    DATA
    //The Timestamp and Hex name are fairly standard, but data can come in any size.
    //Due to this, the plotting and exporting of data has preset options for the user
    //to decide how to handle this. This adds alot of the complexity of these functions

    public partial class MainWindow : System.Windows.Window
    {
        //Global Variables
        string fileToOpen = ""; //User selected file path (points to the data file)
        string fileNameOpened = ""; //Specific filename from the string above
        string exportLocation = ""; //User selected path (points to where the user wants to export the data)
        string fileExtension = ".tsv";
        bool version_control = true; //Determines if the software will look for updates

        string current_version = "2.4.0.0"; //I know... this is not needed. But hey, it was easier to do it this way

        public MainWindow() //Main boot-up function
        {
            InitializeComponent(); //Starts all the components on the window
            Export_cover.Visibility = Visibility.Visible; //There is a grey rectangle that hides all the export components
            //DataVault is a class that houses many variables. The difference is that these variables can be accessed on any form
            //Most of these are for setting default values
            DataVault.IncludeHex = IncludeHex.IsChecked.Value; //Determines if the variable names are to include the Hex number
            DataVault.Number_of_y_plots = 0; //Global counter
            DataVault.AllowInterpolation = false; //I never got this working correctly. It works, but it doesn't make sense in the context of this project
            DataVault.DatapointDots = new bool[] { true, true, true }; //makes all datapoints in the plotter visible by default
            DataVault.LineStyle = new LineStyle[] { LineStyle.Dash, LineStyle.Dash, LineStyle.Dash }; //makes all lines dashed in the plotter by default
            DataVault.PlotSetup = false; //Simple check for error handling. Determines if the plot has been setup yet
            DataVault.LineColors = new string[] { "Auto", "Auto", "Auto"}; //makes all the line colors "auto" by default
            DataVault.LineNames = new string[4]; //Empty array to house custom names for the lines in the plotter
            DataVault.LinePlotMode = new string[] { "Show All Streams", "Show All Streams", "Show All Streams" }; //makes all plotting show all data streams by default
            if (version_control) //Check if we still look for new updates
            {
                try //this section can fail due to internet issues. this catches those issues
                {
                    WebClient client = new WebClient(); //start a web client
                    string webpage_data = client.DownloadString("https://goo.gl/MBmWEm"); //download the webpage with newest version
                    string version = getBetween(webpage_data, "Version: ", "?:)");//get the version number form the website
                    if (current_version == version) //If the versions are the same, we have the newest version
                    {
                        version_box.Text = "Version is up to date"; //write some text
                        update_button.IsEnabled = false; //don't let the user update software
                    }
                    else //version numbers are different, must be a new update
                    {
                        version_box.Text = "A new version is available to download"; //write some text
                        update_button.IsEnabled = true; //Allow the user to click the update button to get new software
                    }
                }
                catch //When an error does occur
                {
                    version_box.Text = "Unable to verify version"; //Write some text
                    update_button.IsEnabled = false; //dont let the user update the software
                }
            }
            else //if the version control is disabled
            {
                version_box.Text = "Version control is disabled"; //write some text 
                update_button.IsEnabled = false; //dont let the user update ... logically
            }
        }

        private void Import_Button_Click(object sender, RoutedEventArgs e) //When the user clicks import
        {
            Loading ld = new Loading(); //create a new loading window
            ld.Show(); //show the loading window
            //the tuple coming back from TsvRead() is in the following format:
            //<HexIDs, Timestamps, Data> coresponding to <Item1, Item2, Item3>
            //since data is variable size, these are all string arrays that are indexed by the HexIDs array.
            //ie:
            // HEXIDs = [0x100, 0x101, 0x102...]
            // Timestamps = ["1\t2\t...", "2\t4\t...", "5\t10\t..." ...] 
            // Data = ["1\t2\t...", "5\t6\t...", "6\t2\t..."]
            // where the data for HEXID 0x101 is "5\t6\t..." and the timestamps are "2\t4\t..."
            //
            // The "\t" is just a way for the data to be split apart into numerical data later
            var data = TsvRead(fileToOpen); //first, we need to open the requested file and get the data
            //the tuple coming back from EnumName() the following format:
            //<Enumerated_Names, Enumerated_Modules> coresponding to <Item1, Item2>
            var enumData = EnumName(data.Item1); //Next, turn the raw hex names into enumerated names
            DataVault.RawData = data.Item3; //Save the raw data to the DataVault
            DataVault.RawTime = data.Item2; //Save the raw timestamp information to the DataVault
            DataVault.SensorNames = enumData.Item1; //Save the Sensor names to the DataVault
            DataVault.ModuleNames = enumData.Item2; //Save the Module names to the DataVault

            //From here on, we are setting up the UI
            Plot_setup_button.IsEnabled = true; //Allow the user to setup a plot
            Export_cover.Visibility = Visibility.Hidden; //Unhide that rectangle that covers up the export options 
            List<string> HexHandle = new List<string>(); //Sets up the user option for how to handle that multiple data streams
            HexHandle.Add("Concatenate All Streams (Dec)");
            HexHandle.Add("Concatenate All Streams (Bin)");
            HexHandle.Add("Show All Streams");
            HexHandle.Add("Add All Streams");
            HexHandle.Add("Subtract All Streams");
            Export_DataType.ItemsSource = HexHandle; //Apply these options to the dropdown menu for the export options
            List<string> SensorList = new List<string>(); //Generate the list of all sensors found in the data file
            for (int i = 0; i < DataVault.SensorNames.Length; i++) //itterate through the sensor names
            {
                SensorList.Add(DataVault.SensorNames[i]); //add the sensor name to the listbox for users to select and export
            }
            Export_data.ItemsSource = SensorList; //add the list of names to the listbox
            if (exportLocation == "") //Check is the export location has been defined
            {
                ExportPath.Text = "Select output file location"; //print some message
            }
            else
            {
                ExportPath.Text = exportLocation; //use the export location as the text in the box
            }
            ld.Close(); //close that loading window. the processing is done
        }

        private void Browse_Button_Click(object sender, RoutedEventArgs e) //When the user clicks the browse button
        {
            var FD = new OpenFileDialog(); //open a file navagator window
            string currentDirectory = Directory.GetCurrentDirectory(); //get the directory
            FD.Filter = "Vehicle Data|*"+fileExtension; //filter all items to just the file type we are looking for
            FD.Title = "Select Vehicle Data File"; //print some message
            if (FD.ShowDialog() == System.Windows.Forms.DialogResult.OK) //if the user selects something and clicks "ok"
            {
                fileToOpen = FD.FileName; //save the filename
                fileNameOpened = System.IO.Path.GetFileNameWithoutExtension(fileToOpen); //remove all the extra stuff so we can have just the filename
            }
            file_path.Text = fileToOpen; //Set the textbox to show the selected file path
            if (file_path.Text != "") //checks if a file was selected
            {
                Import_Button.IsEnabled = true; //if a file is selected, allow the user to import the data
            }
        }

        public string getBetween(string strSource, string strStart, string strEnd) //Small helper method
        {
            //This method returns a string found between two other string from a larger string
            int Start, End;
            if (strSource.Contains(strStart) && strSource.Contains(strEnd))
            {
                Start = strSource.IndexOf(strStart, 0) + strStart.Length;
                End = strSource.IndexOf(strEnd, Start);
                return strSource.Substring(Start, End - Start);
            }
            else
            {
                return "";
            }
        }

        static Tuple<string[], string[], string[]> TsvRead(string fileLocation) //Brings in data from datafile
        {
            using (TextFieldParser parser = new TextFieldParser(fileLocation)) //start out by parsing the file
            {
                //setup the output variables
                string[] Names = new string[1];
                string[] timeStamp = new string[1];
                string[] Datas = new string[1];
                string tempfield = ""; //Since time information is given first, we need to hold onto it in this temp variable
                bool newName = true; //shows if we found a new sensor name or if we have already seen this sensor before
                int nameIndex = 0; //points to the location where each sensor's data and timestamp information is stored
                parser.TextFieldType = FieldType.Delimited; //tab delimited file
                parser.SetDelimiters("\t"); //get rid of the tabs
                int Iteration_count = 1; //Keeps track of the itterations
                while (!parser.EndOfData)
                {
                    //Processing row
                    string[] fields = parser.ReadFields(); //bring in each row of the data file
                    int col_count = 0; //keeps track of the columns in the itteration
                    foreach (string field in fields) //bring in each column
                    {
                        if (col_count == 0) //if this is the first column, we know this is a timestamp
                        {
                            tempfield = field; //hold on to this timestamp until we find out which sensor it belongs to
                        }
                        else if (col_count == 1) //if this is the second column, we know this is a HEX ID. 
                        {
                            if (!Names.Contains(field)) //If this is the first time seeing this ID
                            {
                                newName = true; //Set the New ID flag
                                //This next part is just resizing the output arrays. We only need this to happen when it's not our first iteration
                                if (Iteration_count != 1)
                                {
                                    Array.Resize(ref Names, Names.Length + 1); //increase the size by 1
                                    Array.Resize(ref timeStamp, timeStamp.Length + 1);
                                    Array.Resize(ref Datas, Datas.Length + 1);
                                }
                                Names[Names.Length - 1] = field; //Add the new ID to the end of the array
                                timeStamp[timeStamp.Length - 1] = tempfield; //Save the timestamp we held on to to the timestamp array
                                nameIndex = Array.FindIndex(Names, item => item == field); //find the index for this sensor
                            }
                            else //If we have seen this HEX Id before
                            {
                                newName = false; //set the new ID flag
                                nameIndex = Array.FindIndex(Names, item => item == field); //find out where we've seen this ID before
                                timeStamp[nameIndex] = timeStamp[nameIndex] + "\t" + tempfield; //append the timestamp to the other timestamps for this ID
                            }
                        }
                        else //Any other column after the first two has data in it
                        {
                            if (newName) //If we just found a new name
                            {
                                Datas[Datas.Length - 1] = field; //simply add the data to the end of the array
                                newName = false; //set our new name flag
                            }
                            else //if this is data for a previously found ID or if we have already started adding data
                            {
                                if (field != "") //Make sure there actually is data in the field
                                {
                                    Datas[nameIndex] = Datas[nameIndex] + "\t" + field; //append the data to the other data for this ID
                                }
                            }
                        }
                        col_count++; //increment the column number 
                    }
                    Iteration_count++; //increment the iteration counter
                }
                var tuple = new Tuple<string[], string[], string[]>(Names, timeStamp, Datas); //return the data
                return tuple;
            }
        }

        public Tuple<string[], string[]> EnumName(string[] messageID) //Turn the raw HEX IDs into enumerated names
        {
            //The way we know what the HEX ID names are is based on the the CAN library
            // the library is an excel file the user can edit
            Excel.Application xlApp = new Excel.Application(); //make an excel app
            string currentDirectory = Directory.GetCurrentDirectory();// get the current directory for source files
            string filePath = System.IO.Path.Combine(currentDirectory, "CAN_GPE.xlsx"); //generat the filepath for the library
            Excel.Workbook xlWorkbook = xlApp.Workbooks.Open(filePath); //open the CAN library
            Excel._Worksheet xlWorksheet = xlWorkbook.Sheets[1];//Open the main worksheet
            Excel.Range xlRange = xlWorksheet.UsedRange; //Get the range where there is actually anything on
            int rowCount = xlRange.Rows.Count; //determines how many rows there are
            int colCount = xlRange.Columns.Count; //determines how many columns there are
            string[] Name = new string[rowCount]; //Setup blank array to hold all HEX IDs from the library
            string[] module = new string[rowCount]; //Setup blank array to hold all module names from the library
            string[] enu = new string[rowCount]; //Setup blank array to hold all the enumerated names from the library
            for (int itr_count = 1; itr_count <= rowCount; itr_count++) //itterate over every row
            {
                if (xlRange.Cells[itr_count, 1] != null) //checks if there is actually things in the row
                {
                    //Since we know the library's format of: HEXID, Module Name, Enumerated Name
                    Name[itr_count - 1] = xlRange.Cells[itr_count, 1].Value2.ToString(); //Save the IDs from the library
                    module[itr_count - 1] = xlRange.Cells[itr_count, 2].Value2.ToString(); //Save the module names from the library
                    enu[itr_count - 1] = xlRange.Cells[itr_count, 3].Value2.ToString(); //save the enumerated names from the library
                }
            }
            //This next little section just closes out of the excel file
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Marshal.ReleaseComObject(xlRange);
            Marshal.ReleaseComObject(xlWorksheet);
            xlWorkbook.Close();
            Marshal.ReleaseComObject(xlWorkbook);
            xlApp.Quit();
            Marshal.ReleaseComObject(xlApp);
            string[] enumeratedName = new string[messageID.Length]; //This will be the output array for the enumerated names
            string[] moduleName = new string[messageID.Length]; // this will be the output array for the enumerated modules
            for (int Enum_itr = 1; Enum_itr <= rowCount-1; Enum_itr++) //iterate over the library's HEX Ids
            {
                for(int input_itr = 0; input_itr<= messageID.Length-1; input_itr++) //iterate over the input HEX IDs
                {
                    if(String.Equals(messageID[input_itr], Name[Enum_itr], StringComparison.OrdinalIgnoreCase)) //Checks if the input ID matches the ID found in the library
                    {
                        if (DataVault.IncludeHex) //Checks if the user wants to have the HEX ID included in the enumerated name
                        {
                            enumeratedName[input_itr] = enu[Enum_itr] + " {0x" + messageID[input_itr] + "}"; //Save the enumerated name to the coresponding spot in the output array
                        }
                        else
                        {
                            enumeratedName[input_itr] = enu[Enum_itr]; //Save the enumerated name to the coresponding spot in the output array
                        }
                        moduleName[input_itr] = module[Enum_itr]; //Save the enumerated module to the coresponding spot in the output array
                    }
                }
            }
            //Since it is possible to have a case where a HEX ID is not found in the library, we must make a name for it
            for (int input_itr = 0; input_itr < messageID.Length; input_itr++) //itterate over all the enumerated names
            {
                if(enumeratedName[input_itr] == null) //checks if the name is null
                {
                    if (DataVault.IncludeHex) //Checks if the user wants to add the HEX Id to the name
                    {
                        enumeratedName[input_itr] = "Unnamed_Sensor_" + (input_itr + 1).ToString() + " {0x" + messageID[input_itr] + "}"; //Change the name to something identifiable
                    }
                    else
                    {
                        enumeratedName[input_itr] = "Unnamed_Sensor_" + (input_itr + 1).ToString(); //Change the name to something identifiable
                    }
                    moduleName[input_itr] = "Unnamed_Module_" + (input_itr + 1).ToString(); //Change the module name to something identifiable
                }
            }
            var tuple = new Tuple<string[], string[]>(enumeratedName, moduleName);//return the data
            return tuple;
        }

        private void update_button_Click(object sender, RoutedEventArgs e) //When the user clicks the update software button
        {
            //opens up webpage to get new software
            System.Diagnostics.Process.Start("https://bit.ly/2GzkzjH");
        }

        private int[] data_expander(string data, bool isHex) //This method takes raw data and turns it into a number array
        {
            string[] parts = data.Split('\t'); //take the raw data and split it into individual strings
            int[] output = new int[parts.Length]; //setup output variable
            int Output_itr = 0; //keeps track of the iteration
            foreach (var word in parts) //iterate over every string
            {
                if (word != "") //make sure the string has stuff in it
                {
                    if (isHex) //checks if the user wants the data to be converted from HEX to dec
                    {
                        output[Output_itr] = int.Parse(word, System.Globalization.NumberStyles.HexNumber); //turn the string into a number and save it to the output variable
                    }
                    else
                    {
                        output[Output_itr] = int.Parse(word); //turn the string into a number and save it to the output variable
                    }
                    Output_itr++; //incredment the count
                }
            }
            return output; //return the data
        }

        private void Data_plot_button_Click(object sender, RoutedEventArgs e) //When the user clicks the plot button
        {
            if (!DataVault.PlotSetup) //Checks if there is a plot setup
            {
                Error err = new Error("No Plot Setup", "Please setup plot before attempting to plot"); //bring up error message
                err.Show();//show message
            }
            else
            {
                Loading ld = new Loading(); //make loading window
                ld.Show(); //show loading window
                var plot = Make_plot(DataVault.PlotRequestX, DataVault.PlotRequestY); //Calls the Make_Plot() function to generate plot
                MainViewModel.mdl = plot; // load in the plot model
                ld.Close(); // close the loading window
                Plot plt = new Plot(); //bring up the plot window
                plt.Show(); //show the plot window
            }
        }

        private void Plot_setup_button_Click(object sender, RoutedEventArgs e) //When the user clicks the plot setup button
        {
            Plot_setup pltset = new Plot_setup(); //bring up plot setup window
            pltset.Show(); //show window
            Data_plot_button.IsEnabled = true; //allow the user to click on the plot button
        }

        private PlotModel Make_plot(string x_name, string[] y_name) //Generates the plot
        {
            int x_pos = 0; //Stores the index for the X-axis
            int y_pos = 0; //Stores the index for the Y-axis
            int[] x_axis = new int[] { }; //empty array for the x_axis
            int[] y_axis = new int[] { }; //empty array for the y_axis
            List<DataPoint> Points = new List<DataPoint>(); //empty set of datapoints
            var plot = new PlotModel(); //make a new plot model (this will allow us to send the datapoints)

            for (int y_req = 0; y_req < y_name.Length; y_req++) //itterate over all y-axis. The user can have one, two, or three
            {
                LineStyle set_Linestyle = DataVault.LineStyle[y_req]; //Stores the user requested linestyle (things like dashes or solid lines)
                MarkerType set_marker = new MarkerType(); //Stores the user datapoint marker type
                if (DataVault.DatapointDots[y_req]) //checks if the user has requested the datapoints have dots
                {
                    set_marker = MarkerType.Circle; //sets the marker to have dots
                }
                else
                {
                    set_marker = MarkerType.None; //sets the marker to be invisble
                }
                int PlotMode = RequestedLinePlotMode(y_req); //Calls a function that will determine how to handle to data streams
                for (int name_itr = 0; name_itr < DataVault.SensorNames.Length; name_itr++) //iterate over all sensor names to find the ones that were requested
                {
                    if (DataVault.SensorNames[name_itr] == x_name) //check the x-axis
                    {
                        x_pos = name_itr;
                    }
                    if (DataVault.SensorNames[name_itr] == y_name[y_req]) //check the y-axis
                    {
                        y_pos = name_itr;
                    }
                }
                if (x_pos == 0)//if time is the x axis (which can't be changed)
                {
                    x_axis = data_expander(DataVault.RawTime[y_pos], false); //expand the raw timestamps and treat them as regular DEC numbers
                    y_axis = data_expander(DataVault.RawData[y_pos], true); //epand the raw data and treat them as HEX numbers
                    if (PlotMode == 2) //if the user selected "Plot data streams seperately"
                    {
                        if (y_axis.Length / x_axis.Length > 1) //Check if there is more than one stream
                        {
                            //What happens here is that every datapoint needs to get put in its own scatterplot
                            //We have to first iterate over all the datastreams and then every timestamp
                            //ie
                            //TimeStamp   HEXID   DATA
                            // 1           101     1  2  3  4
                            // 2           101     1  2  4  5
                            // 3           101     1  4  4  3
                            // 
                            // should make four line series containing:
                            // Series1 = {(1,1), (2,1), (3,1)}
                            // Series2 = {(1,2), (2,2), (3,4)}
                            // Series3 = {(1,3), (2,4), (3,4)}
                            // Series4 = {(1,4), (2,5), (3,3)}
                            int ratio = y_axis.Length / x_axis.Length; //this variable is used to calculate which datapoints belong to which timestamp
                            for (int data_stream_itr = 0; data_stream_itr < ratio - 1; data_stream_itr++) //iterate over the number of data streams
                            {
                                var linePoints = new DataPoint[x_axis.Length - 1]; //setup blank datapoints variable (this is a special type that holds discrete datapoints)
                                for (int x_count = 1; x_count < x_axis.Length + 1; x_count++) //iterate over the entire x array
                                {
                                    try
                                    {
                                        linePoints[x_count - 1] = new DataPoint(x_axis[x_count - 1], y_axis[x_count * ratio + (data_stream_itr - (ratio - 2))]); //create a datapoint
                                    }
                                    catch (IndexOutOfRangeException)
                                    {
                                        break;
                                    }
                                }
                                string th_or_st = "st"; //I like to have the names in the legend of the plot look nice. This if/else section determines the correct ending for numbers
                                if (data_stream_itr == 0)
                                {
                                    th_or_st = "st"; //if it's the first series
                                }
                                else if (data_stream_itr == 1)
                                {
                                    th_or_st = "nd"; //if it's the second series
                                }
                                else if (data_stream_itr == 2)
                                {
                                    th_or_st = "rd"; //if it's the third series
                                }
                                else
                                {
                                    th_or_st = "th"; //for all the rest
                                }
                                var lineSeries = new LineSeries //Define the line series using the data points we just found
                                {
                                    Title = data_stream_itr + 1 + th_or_st + " Data Series of " + DataVault.LineNames[y_req],
                                    Color = RequestedLineColor(y_req),
                                    StrokeThickness = 2,
                                    ItemsSource = linePoints,
                                    MarkerSize = 2,
                                    MarkerType = set_marker,
                                    LineStyle = set_Linestyle
                                }; //This is also where we can change different parameters about the line
                                plot.Series.Add(lineSeries); //Add the series to the plot model
                            }
                        }
                        else
                        {
                            //Not every sensor has multiple data streams. This simplifies things for those lonely sensors
                            var linePoints = new DataPoint[x_axis.Length - 1]; //set up a blank set of linepoints
                            for (int x_itr = 0; x_itr < x_axis.Length - 1; x_itr++) //iterate over the entire x_axis
                            {
                                linePoints[x_itr] = new DataPoint(x_axis[x_itr], y_axis[x_itr]); //Make the new datapoint
                            }
                            var lineSeries = new LineSeries //setup line series using the datapoints we just created
                            {
                                Title = DataVault.LineNames[y_req],
                                Color = RequestedLineColor(y_req),
                                StrokeThickness = 2,
                                ItemsSource = linePoints,
                                MarkerSize = 2,
                                MarkerType = set_marker,
                                LineStyle = set_Linestyle
                            };
                            plot.Series.Add(lineSeries); //add the series to the plot model
                        }
                    }
                    else
                    { //In this section, each sensor produces one line series
                        if (x_axis.Length < y_axis.Length) //Check if there is more than one stream
                        {
                            var linePoints = new DataPoint[x_axis.Length - 1]; //make a blank set of datapoints
                            int ratio = y_axis.Length / x_axis.Length; //used to find where the data streams end
                            int y_sum = 0; //These calculations need to be accumulated into one variable
                            string y_sum_string = ""; //some cases this is more helpful than using an int
                            for (int data_itr = 1; data_itr < y_axis.Length + 1; data_itr++) //iterate over the entire data set
                            {
                                try
                                {
                                    for (int data_stream_itr = 0; data_stream_itr < ratio; data_stream_itr++) //iterate over each data stream
                                    {
                                        if (PlotMode == 1) //the user selected "Concatenate streams DEC"
                                        {
                                            y_sum_string = (y_axis[data_itr * ratio - data_stream_itr - 1].ToString() + y_sum_string); //concatenates the data stream
                                        }
                                        else if (PlotMode == 3) //the user selected "add the streams"
                                        {
                                            y_sum = y_sum + y_axis[data_itr * ratio - data_stream_itr - 1]; //Adds the data stream
                                        }
                                        else if (PlotMode == 4) //the user selected "subtract the streams"
                                        {
                                            y_sum = y_sum - y_axis[data_itr * ratio - data_stream_itr - 1]; //Subtracts the data stream
                                        }
                                        else if (PlotMode == 5) //the user selected "Concatenate streams BIN"
                                        {
                                            y_sum_string = (Convert.ToString(y_axis[data_itr * ratio - data_stream_itr - 1], 2) + y_sum_string); //converts the data to binary then concatenates the data stream
                                        }
                                    }
                                    Int64 conversion_int = 0; //Just a variable to convert the strings to ints
                                    if (PlotMode == 5) //if the number is in binary, we need to convert that before plotting
                                    {
                                        conversion_int = ConvertClass.Convert(y_sum_string); //convert to decimal
                                    }
                                    else if (PlotMode == 1)
                                    {
                                        conversion_int = Int64.Parse(y_sum_string); // change the string to int
                                    }
                                    else
                                    {
                                        conversion_int = y_sum; //set the variable to the int
                                    }
                                    linePoints[data_itr - 1] = new DataPoint(x_axis[data_itr - 1], conversion_int); //create the datapoint
                                    y_sum = 0; //reset the variables 
                                    y_sum_string = "";
                                }
                                catch (IndexOutOfRangeException)
                                {
                                    break;
                                }
                            }
                            var lineSeries = new LineSeries //generate the line series using the datapoints that were just found
                            {
                                StrokeThickness = 2,
                                Color = RequestedLineColor(y_req),
                                Title = DataVault.LineNames[y_req],
                                ItemsSource = linePoints,
                                MarkerSize = 2,
                                MarkerType = set_marker,
                                LineStyle = set_Linestyle
                            };
                            plot.Series.Add(lineSeries); //add the series to the plot model
                        }
                        else //if there is only one data stream
                        {
                            var linePoints = new DataPoint[x_axis.Length - 1]; //make a blank set of datapoints
                            for (int i = 1; i < x_axis.Length; i++) //iterate over the data set
                            {
                                linePoints[i - 1] = new DataPoint(x_axis[i - 1], y_axis[i - 1]); //create the data point
                            }
                            var lineSeries = new LineSeries //generate line series for the datapoints just found
                            {
                                StrokeThickness = 2,
                                Color = RequestedLineColor(y_req),
                                Title = DataVault.LineNames[y_req],
                                ItemsSource = linePoints,
                                MarkerSize = 2,
                                MarkerType = set_marker,
                                LineStyle = set_Linestyle
                            };
                            plot.Series.Add(lineSeries); //add the series to the plot model
                        }
                    }
                }
                else //If time is not the X-axis (right now this is not available)
                {
                    //In order to do this, we need to use an interpolation algorithm. Essentially:
                    // 1. Get a function describing the x-axis
                    // 2. Solve the function in terms of time (essentially, we need this function to take in a timestamp and return a datapoint)
                    // 3. Use each timestamp for the y-axis variable and plug it into the function
                    // 4. Done! this is the x coordinate for the datapoint for plotting
                    // the code is the same (for the most part) as the X-axis stuff above

                    //Probably should have this as a changeable option, but I decided not to
                    //in reality, this should return a correct shape graph, but the numbers on the X-axis might be off
                    int Data_interm_mode = 2;

                    int[] y_time = new int[] { }; //empty array for the x_axis
                    int[] y_data = new int[] { }; //empty array for the y_axis
                    y_time = data_expander(DataVault.RawTime[y_pos], false); //expand the raw timestamps and treat them as regular DEC numbers
                    y_data = data_expander(DataVault.RawData[y_pos], true); //epand the raw data and treat them as HEX numbers
                    int[] x_time = new int[] { }; //empty array for the x_axis
                    int[] x_data = new int[] { }; //empty array for the y_axis
                    x_time = data_expander(DataVault.RawTime[x_pos], false); //expand the raw timestamps and treat them as regular DEC numbers
                    x_data = data_expander(DataVault.RawData[x_pos], true); //epand the raw data and treat them as HEX numbers

                    //double[] x_timedouble = new double[x_time.Length];
                    //double[] x_datadouble = new double[x_time.Length];

                    if (x_data.Length / x_time.Length > 1)
                    {
                        int y_sum = 0; //These calculations need to be accumulated into one variable
                        string y_sum_string = ""; //some cases this is more helpful than using an int
                        int ratio = x_data.Length / x_time.Length;
                        for (int data_itr = 1; data_itr < x_data.Length + 1; data_itr++) //iterate over the entire data set
                        {
                            try
                            {
                                for (int data_stream_itr = 0; data_stream_itr < ratio; data_stream_itr++) //iterate over each data stream
                                {
                                    if (Data_interm_mode == 1) //"Concatenate streams DEC"
                                    {
                                        y_sum_string = (x_data[data_itr * ratio - data_stream_itr - 1].ToString() + y_sum_string); //concatenates the data stream
                                    }
                                    else if (Data_interm_mode == 2) //"add the streams"
                                    {
                                        y_sum = y_sum + (int)x_data[data_itr * ratio - data_stream_itr - 1]; //Adds the data stream
                                    }
                                    else if (Data_interm_mode == 3) //"subtract the streams"
                                    {
                                        y_sum = y_sum - (int)x_data[data_itr * ratio - data_stream_itr - 1]; //Subtracts the data stream
                                    }
                                }
                                Int64 conversion_int = 0; //Just a variable to convert the strings to ints
                                if (Data_interm_mode == 1)
                                {
                                    conversion_int = Int64.Parse(y_sum_string); // change the string to int
                                }
                                else
                                {
                                    conversion_int = y_sum; //set the variable to the int
                                }
                                //x_datadouble[data_itr - 1] = x_data[data_itr-1];
                                y_sum = 0; //reset the variables 
                                y_sum_string = "";
                            }
                            catch (IndexOutOfRangeException)
                            {
                                break;
                            }
                        }
                        for (int i = 0; i < x_time.Length; i++)
                        {
                            //x_timedouble[i] = x_time[i];
                        }
                    }
                    else
                    {
                        for (int i = 0; i < x_time.Length; i++)
                        {
                            //x_timedouble[i] = x_time[i];
                            //x_datadouble[i] = x_data[i];
                        }
                    }

                    //try
                    //{
                        //var spline = Interpolate.Polynomial(x_timedouble, x_datadouble);

                        double[] x_axis_new = new double[y_time.Length];
                        double[] y_axis_new = new double[y_data.Length];

                        for (int i = 0; i < y_time.Length; i++)
                        {
                            x_axis_new[i] = Interpolate_Array(x_time, x_data, y_time[i], PlotMode);
                        }
                        for (int i = 0; i < y_data.Length; i++)
                        {
                            y_axis_new[i] = y_data[i];
                        }


                        if (PlotMode == 2) //if the user selected "Plot data streams seperately"
                        {
                            if (y_axis_new.Length / x_axis_new.Length > 1) //Check if there is more than one stream
                            {
                                //What happens here is that every datapoint needs to get put in its own scatterplot
                                //We have to first iterate over all the datastreams and then every timestamp
                                //ie
                                //TimeStamp   HEXID   DATA
                                // 1           101     1  2  3  4
                                // 2           101     1  2  4  5
                                // 3           101     1  4  4  3
                                // 
                                // should make four line series containing:
                                // Series1 = {(1,1), (2,1), (3,1)}
                                // Series2 = {(1,2), (2,2), (3,4)}
                                // Series3 = {(1,3), (2,4), (3,4)}
                                // Series4 = {(1,4), (2,5), (3,3)}
                                int ratio = y_axis_new.Length / x_axis_new.Length; //this variable is used to calculate which datapoints belong to which timestamp
                                for (int data_stream_itr = 0; data_stream_itr < ratio - 1; data_stream_itr++) //iterate over the number of data streams
                                {
                                    var linePoints = new DataPoint[x_axis_new.Length - 1]; //setup blank datapoints variable (this is a special type that holds discrete datapoints)
                                    for (int x_count = 1; x_count < x_axis_new.Length + 1; x_count++) //iterate over the entire x array
                                    {
                                        try
                                        {
                                            linePoints[x_count - 1] = new DataPoint(x_axis_new[x_count - 1], y_axis_new[x_count * ratio + (data_stream_itr - (ratio - 2))]); //create a datapoint
                                        }
                                        catch (IndexOutOfRangeException)
                                        {
                                            break;
                                        }
                                    }
                                    string th_or_st = "st"; //I like to have the names in the legend of the plot look nice. This if/else section determines the correct ending for numbers
                                    if (data_stream_itr == 0)
                                    {
                                        th_or_st = "st"; //if it's the first series
                                    }
                                    else if (data_stream_itr == 1)
                                    {
                                        th_or_st = "nd"; //if it's the second series
                                    }
                                    else if (data_stream_itr == 2)
                                    {
                                        th_or_st = "rd"; //if it's the third series
                                    }
                                    else
                                    {
                                        th_or_st = "th"; //for all the rest
                                    }
                                    var lineSeries = new LineSeries //Define the line series using the data points we just found
                                    {
                                        Title = data_stream_itr + 1 + th_or_st + " Data Series of " + DataVault.LineNames[y_req],
                                        Color = RequestedLineColor(y_req),
                                        StrokeThickness = 2,
                                        ItemsSource = linePoints,
                                        MarkerSize = 2,
                                        MarkerType = set_marker,
                                        LineStyle = set_Linestyle
                                    }; //This is also where we can change different parameters about the line
                                    plot.Series.Add(lineSeries); //Add the series to the plot model
                                }
                            }
                            else
                            {
                                //Not every sensor has multiple data streams. This simplifies things for those lonely sensors
                                var linePoints = new DataPoint[x_axis_new.Length - 1]; //set up a blank set of linepoints
                                for (int x_itr = 0; x_itr < x_axis_new.Length - 1; x_itr++) //iterate over the entire x_axis
                                {
                                    linePoints[x_itr] = new DataPoint(x_axis_new[x_itr], y_axis_new[x_itr]); //Make the new datapoint
                                }
                                var lineSeries = new LineSeries //setup line series using the datapoints we just created
                                {
                                    Title = DataVault.LineNames[y_req],
                                    Color = RequestedLineColor(y_req),
                                    StrokeThickness = 2,
                                    ItemsSource = linePoints,
                                    MarkerSize = 2,
                                    MarkerType = set_marker,
                                    LineStyle = set_Linestyle
                                };
                                plot.Series.Add(lineSeries); //add the series to the plot model
                            }
                        }
                        else
                        { //In this section, each sensor produces one line series
                            if (x_axis_new.Length < y_axis_new.Length) //Check if there is more than one stream
                            {
                                var linePoints = new DataPoint[x_axis_new.Length - 1]; //make a blank set of datapoints
                                int ratio = y_axis_new.Length / x_axis_new.Length; //used to find where the data streams end
                                int y_sum = 0; //These calculations need to be accumulated into one variable
                                string y_sum_string = ""; //some cases this is more helpful than using an int
                                for (int data_itr = 1; data_itr < y_axis_new.Length + 1; data_itr++) //iterate over the entire data set
                                {
                                    try
                                    {
                                        for (int data_stream_itr = 0; data_stream_itr < ratio; data_stream_itr++) //iterate over each data stream
                                        {
                                            if (PlotMode == 1) //the user selected "Concatenate streams DEC"
                                            {
                                                y_sum_string = (y_axis_new[data_itr * ratio - data_stream_itr - 1].ToString() + y_sum_string); //concatenates the data stream
                                            }
                                            else if (PlotMode == 3) //the user selected "add the streams"
                                            {
                                                y_sum = y_sum + (int)y_axis_new[data_itr * ratio - data_stream_itr - 1]; //Adds the data stream
                                            }
                                            else if (PlotMode == 4) //the user selected "subtract the streams"
                                            {
                                                y_sum = y_sum - (int)y_axis_new[data_itr * ratio - data_stream_itr - 1]; //Subtracts the data stream
                                            }
                                            else if (PlotMode == 5) //the user selected "Concatenate streams BIN"
                                            {
                                                Error er = new Error("Fatal Error", "Choose a different Hex Handler! BIN not \r supported for interpolation"); //Can't get the thing to work so i got rid of it :)
                                                er.Show();
                                                return plot;
                                                //y_sum_string = (Convert.ToString(y_axis[data_itr * ratio - data_stream_itr - 1], 2) + y_sum_string); //converts the data to binary then concatenates the data stream
                                            }
                                        }
                                        Int64 conversion_int = 0; //Just a variable to convert the strings to ints
                                        if (PlotMode == 5) //if the number is in binary, we need to convert that before plotting
                                        {
                                            conversion_int = ConvertClass.Convert(y_sum_string); //convert to decimal
                                        }
                                        else if (PlotMode == 1)
                                        {
                                            conversion_int = Int64.Parse(y_sum_string); // change the string to int
                                        }
                                        else
                                        {
                                            conversion_int = y_sum; //set the variable to the int
                                        }
                                        linePoints[data_itr - 1] = new DataPoint(x_axis_new[data_itr - 1], conversion_int); //create the datapoint
                                        y_sum = 0; //reset the variables 
                                        y_sum_string = "";
                                    }
                                    catch (IndexOutOfRangeException)
                                    {
                                        break;
                                    }
                                }
                                var lineSeries = new LineSeries //generate the line series using the datapoints that were just found
                                {
                                    StrokeThickness = 2,
                                    Color = RequestedLineColor(y_req),
                                    Title = DataVault.LineNames[y_req],
                                    ItemsSource = linePoints,
                                    MarkerSize = 2,
                                    MarkerType = set_marker,
                                    LineStyle = set_Linestyle
                                };
                                plot.Series.Add(lineSeries); //add the series to the plot model
                            }
                            else //if there is only one data stream
                            {
                                var linePoints = new DataPoint[x_axis_new.Length - 1]; //make a blank set of datapoints
                                for (int i = 1; i < x_axis_new.Length; i++) //iterate over the data set
                                {
                                    linePoints[i - 1] = new DataPoint(x_axis_new[i - 1], y_axis_new[i - 1]); //create the data point
                                }
                                var lineSeries = new LineSeries //generate line series for the datapoints just found
                                {
                                    StrokeThickness = 2,
                                    Color = RequestedLineColor(y_req),
                                    Title = DataVault.LineNames[y_req],
                                    ItemsSource = linePoints,
                                    MarkerSize = 2,
                                    MarkerType = set_marker,
                                    LineStyle = set_Linestyle
                                };
                                plot.Series.Add(lineSeries); //add the series to the plot model
                            }
                        }
                    //}
                    //catch
                    //{
                        //Error er = new Error("Fatal Error!", "System could not process the request");
                        //er.Show();
                        //return plot;
                   //}
                }
            }
            plot.LegendTitle = "Legend"; //Adds a legend to the plot
            plot.LegendPosition = LegendPosition.LeftTop; //position the legend to the top left of the screen
            var xAxis = new OxyPlot.Axes.LinearAxis //make a x-axis
            {
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.None,
            };

            var yAxis = new OxyPlot.Axes.LinearAxis //make a y-axis
            {
                Position = OxyPlot.Axes.AxisPosition.Left,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.None
            };
            plot.Axes.Add(xAxis); //put the axis on the plot
            plot.Axes.Add(yAxis);
            return plot; //return the plot
        }

        private int RequestedLinePlotMode(int index, string optionalVar = "") //Generates a plotting mode
        {
            if (string.IsNullOrWhiteSpace(optionalVar))//the optional variable is just for the case when we need to switch something that is not stored in the DataVault
            {
                switch (DataVault.LinePlotMode[index]) //bring in the requested plot mode string
                {
                    case ("Concatenate All Streams (Dec)"): //change the strings into a coresponding number
                        return 1;
                    case ("Show All Streams"):
                        return 2;
                    case ("Add All Streams"):
                        return 3;
                    case ("Subtract All Streams"):
                        return 4;
                    case ("Concatenate All Streams (Bin)"):
                        return 5;
                    default:
                        return 0;
                }
            }
            else
            {
                switch (optionalVar) //Same as above, but switching on the optional variable
                {
                    case ("Concatenate All Streams (Dec)"):
                        return 1;
                    case ("Show All Streams"):
                        return 2;
                    case ("Add All Streams"):
                        return 3;
                    case ("Subtract All Streams"):
                        return 4;
                    case ("Concatenate All Streams (Bin)"):
                        return 5;
                    default:
                        return 0;
                }
            }
        }

        private void Table_button_Click(object sender, RoutedEventArgs e) //When the user selects the export button
        {
            if (Export_data.SelectedItems.Count == 0) //checks if there is no sensor selected to export
            {
                Error err = new Error("Error", "Select Sensors from the list before exporting"); //generate error window
                err.Show(); //display error message
            }
            else
            {
                int ExportMode = RequestedLinePlotMode(0, Export_DataType.SelectedItem.ToString()); //determine the data stream mode
                var RequestedSensors = Export_data.SelectedItems; //sensors that were selected to be exported
                using (ExcelPackage excel = new ExcelPackage()) //make a new excel workbook
                {
                    for (int Req_sensor_itr = 0; Req_sensor_itr < RequestedSensors.Count; Req_sensor_itr++) //iterate over all requested sensor
                    {
                        string RequestedSensor = RequestedSensors[Req_sensor_itr].ToString(); //Get sensor name
                        RequestedSensor = RequestedSensor.Replace('/', '_'); //Remove slashes. They aren't friendly to excel
                        RequestedSensor = RequestedSensor.Truncate(31); //Limit the size of the name. Names longer than 31 cause a crash
                        excel.Workbook.Worksheets.Add(RequestedSensor); //Make a worksheet with the sensor name

                        var excelWorksheet = excel.Workbook.Worksheets[RequestedSensor]; //generate sheet

                        List<string[]> headerRow = new List<string[]>() //Make list for the headers on each sheet
                        {
                            new string[] { "Time Stamp", "Variable Name", "Module Name", "Data" }
                        };
                        string headerRange = "A1:" + Char.ConvertFromUtf32(headerRow[0].Length + 64) + "1"; //Calculates the range for the header
                        excelWorksheet.Cells[headerRange].LoadFromArrays(headerRow); //Apply the header to the sheet
                        int y_pos = 0; //Determines the index for the requested sensor
                        for (int SensorList_itr = 0; SensorList_itr < DataVault.SensorNames.Length; SensorList_itr++) //iterate over all sensors
                        {
                            if (DataVault.SensorNames[SensorList_itr] == RequestedSensors[Req_sensor_itr].ToString()) //Check where the requested sensor exists in the array of names
                            {
                                y_pos = SensorList_itr; //save that index
                            }
                        }
                        string moduleName = DataVault.ModuleNames[y_pos]; //generate an array to save the requested sensor's module
                        int[] timeStamps = new int[] { }; //empty array for the time stamps
                        int[] dataPoints = new int[] { }; //empty array for the datapoints
                        timeStamps = data_expander(DataVault.RawTime[y_pos], false); //turn the raw timestamps for the sensor into usable data
                        dataPoints = data_expander(DataVault.RawData[y_pos], true); //turn the raw data for the sensor into usable data
                        if (ExportMode == 2) //check if the user selected to show each data stream
                        {
                            if (timeStamps.Length < dataPoints.Length) //Chech if there is more than one stream
                            {
                                int ratio = dataPoints.Length / timeStamps.Length; //intermediate calculation for how many data streams there are
                                for (int rowc_itr = 1; rowc_itr < timeStamps.Length + 1; rowc_itr++) //iterate over every timestamp (this should reflect how many rows we will need)
                                {
                                    int iteration_count = 1; //Keeps track of how many iterations we have
                                    List<string[]> RowData = new List<string[]>(); //make an empty list for the row data
                                    if (rowc_itr * ratio + (0 - (ratio - 1)) < dataPoints.Length) //make sure we don't exceed the array size
                                    {
                                        //The next line sets up the data that will be added to the excel form.
                                        RowData.Add(new string[] { timeStamps[rowc_itr-1].ToString(), RequestedSensors[Req_sensor_itr].ToString(), moduleName, dataPoints[rowc_itr * ratio - (ratio - 1)].ToString() });
                                        string dataRange = "A" + (rowc_itr + 1).ToString() + ":" + Char.ConvertFromUtf32(RowData[0].Length + 64) + (rowc_itr + 1).ToString(); //Determines what range the data will take up
                                        excelWorksheet.Cells[dataRange].LoadFromArrays(RowData); //applys the row to the excel form
                                        for (int Ratio_itr = 0; Ratio_itr < ratio - 1; Ratio_itr++) //iterate over each data stream
                                        { //The logic for this section is that we add the extra data streams to the cells just to the right to the cells we just defined above
                                            try
                                            {
                                                List<string[]> ExcessData = new List<string[]>() //make the single cell with the extra data steams
                                                {
                                                    new string[] { dataPoints[rowc_itr * ratio + (Ratio_itr - (ratio - 1))].ToString() }
                                                };
                                                string excessRange = Char.ConvertFromUtf32(RowData[0].Length + 64 + iteration_count) + (rowc_itr + 1).ToString(); //calculate what cell is directly to the right of the data that was just added
                                                excelWorksheet.Cells[excessRange].LoadFromArrays(ExcessData); //apply the new cell to the workbook
                                                iteration_count++; //increment the iteration
                                            }
                                            catch (IndexOutOfRangeException)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            else //when there is just one data stream
                            {
                                for (int row_itr = 1; row_itr < dataPoints.Length + 1; row_itr++) //iterate over all timestamps
                                {
                                    List<string[]> RowData = new List<string[]>() //Formats the data for the row
                                    {
                                        new string[] { timeStamps[row_itr-1].ToString(), RequestedSensors[Req_sensor_itr].ToString(), moduleName, dataPoints[row_itr-1].ToString() }
                                    };
                                    string dataRange = "A" + (row_itr + 1).ToString() + ":" + Char.ConvertFromUtf32(RowData[0].Length + 64) + (row_itr + 1).ToString(); //calculates what range this row will be applied to
                                    excelWorksheet.Cells[dataRange].LoadFromArrays(RowData); //apply the row to the sheet
                                }
                            }
                        }
                        else
                        {
                            if (timeStamps.Length < dataPoints.Length) //check if there is more than one stream
                            {
                                int ratio = dataPoints.Length / timeStamps.Length; //find out how many streams there are
                                int y_sum = 0; //variable to house the incremental data
                                string y_sum_string = ""; //same as the variable above, but some cases a string works better than the int
                                for (int data_itr = 1; data_itr < dataPoints.Length + 1; data_itr++) //iterate over all data
                                {
                                    try
                                    {
                                        for (int ratio_itr = 0; ratio_itr < ratio; ratio_itr++) //iterate over each data stream
                                        {
                                            if (ExportMode == 1) //if the user selected concatenate all data streams
                                            {
                                                y_sum_string = (dataPoints[data_itr * ratio - ratio_itr - 1].ToString() + y_sum_string); //concatenates all data stream
                                            }
                                            else if (ExportMode == 3) //if the user selected sum all data streams
                                            {
                                                y_sum = y_sum + dataPoints[data_itr * ratio - ratio_itr - 1]; //sum the data streams
                                            }
                                            else if (ExportMode == 4) //if the user selected subtract all data streams
                                            {
                                                y_sum = y_sum - dataPoints[data_itr * ratio - ratio_itr - 1]; //subtract the data streams
                                            }
                                            else if (ExportMode == 5) //if the user selected concatenate all data streams (bin)
                                            {
                                                y_sum_string = (Convert.ToString(dataPoints[data_itr * ratio - ratio_itr - 1], 2) + y_sum_string); //concatenate the binary version of each number
                                            }
                                        }
                                        string conversion_string = ""; //sets up an intermediate string
                                        if (ExportMode == 1 || ExportMode == 5) //checks if we are in a mode that used y_sum_string
                                        {
                                            conversion_string = y_sum_string; //set the intermediate string to the incremental value
                                        }
                                        else
                                        {
                                            conversion_string = y_sum.ToString(); //set the intermediate string to the incremental value
                                        }
                                        List<string[]> RowData = new List<string[]>() //generate the row data for the excel sheet
                                        {
                                            new string[] { timeStamps[data_itr-1].ToString(), RequestedSensors[Req_sensor_itr].ToString(), moduleName, conversion_string } //list is formated to line up with the header
                                        };
                                        string dataRange = "A" + (data_itr + 1).ToString() + ":" + Char.ConvertFromUtf32(RowData[0].Length + 64) + (data_itr + 1).ToString(); //determines the range for the new row
                                        excelWorksheet.Cells[dataRange].LoadFromArrays(RowData); //apply the new row to the excel sheet
                                        y_sum = 0; //reset the variables
                                        y_sum_string = "";
                                    }
                                    catch (IndexOutOfRangeException)
                                    {
                                        break;
                                    }
                                }
                            }
                            else //for when there is only one data stream
                            {
                                for (int data_itr = 1; data_itr < dataPoints.Length + 1; data_itr++) //iterate over all datapoints
                                {
                                    string conversion_string = ""; //empty string to house the data
                                    if (ExportMode == 5)
                                    {
                                        conversion_string = Convert.ToString(dataPoints[data_itr - 1], 2); //conver the data to binary
                                    }
                                    else
                                    {
                                        conversion_string = dataPoints[data_itr - 1].ToString(); //convert the data to a string
                                    }
                                    List<string[]> RowData = new List<string[]>() //make new row data
                                    {
                                        new string[] { timeStamps[data_itr-1].ToString(), RequestedSensors[Req_sensor_itr].ToString(), moduleName, conversion_string }//formated list for the excel document
                                    };
                                    string dataRange = "A" + (data_itr + 1).ToString() + ":" + Char.ConvertFromUtf32(RowData[0].Length + 64) + (data_itr + 1).ToString(); //determines the range for the new list
                                    excelWorksheet.Cells[dataRange].LoadFromArrays(RowData); //apply the new row to the excel sheet
                                }
                            }
                        }
                    }
                    string curFile = exportLocation + "/Processed_" + fileNameOpened + ".xlsx"; //make the name of the export file
                    int file_counter = 1; //counter to allow for multiple file creation
                    while (File.Exists(curFile)) //check if there is a file with the same name
                    {
                        curFile = exportLocation + "/Processed_" + fileNameOpened + "_(" + file_counter.ToString() + ").xlsx"; //if a file exists, we need to append a number after the name
                        file_counter++; //increment the file counter number
                    }
                    FileInfo excelFile = new FileInfo(curFile); //create the excel file at that location with that name
                    excel.SaveAs(excelFile); //save the excel file
                    System.Windows.Forms.MessageBox.Show("File has been exported to: " + curFile); //send a note to the user that the file has been created
                }
            }
        }

        private OxyColor RequestedLineColor(int index) //returns the requested color
        {
            switch(DataVault.LineColors[index]) //looks up the requested color and changes the string to a color type
            {
                case ("Yellow"):
                    return OxyColors.Yellow;
                case ("White"):
                    return OxyColors.White;
                case ("Red"):
                    return OxyColors.Red;
                case ("Blue"):
                    return OxyColors.Blue;
                case ("Green"):
                    return OxyColors.Green;
                case ("Orange"):
                    return OxyColors.Orange;
                case ("Indigo"):
                    return OxyColors.Indigo;
                case ("Black"):
                    return OxyColors.Black;
                default:
                    return OxyColors.Automatic;
            }
            
        }

        private void Open_Cal_Click(object sender, RoutedEventArgs e) //when the user selects the calibration file button
        {
            string currentDirectory = Directory.GetCurrentDirectory(); //determines the path to the excel file
            System.Windows.Forms.MessageBox.Show("Save and Close the file once changes have been made. Click the 'Import' button to re-run."); //helpful hint box pops open to instruct the user on what to do
            System.Diagnostics.Process.Start("CAN_GPE.xlsx", currentDirectory); //opens file
        }

        private void Export_location_Click(object sender, RoutedEventArgs e) //When the user selects the export location button
        {
            using (var fbd = new FolderBrowserDialog()) //open folder dialog box
            {
                DialogResult result = fbd.ShowDialog(); //get the resulting path
                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath)) //make sure the ok button is pressed as well as the path is not just a bunch of spaces
                {
                    exportLocation = fbd.SelectedPath; //set export location to the selected path
                    Table_button.IsEnabled = true; //turn on the export button
                    ExportPath.Text = exportLocation; //update the textbox to show selected location
                }
            }
        }

        private void IncludeHex_Checked(object sender, RoutedEventArgs e) //when the user clicks the checkbox to include hex values
        {
            DataVault.IncludeHex = IncludeHex.IsChecked.Value; //update global boolean
            if (fileToOpen != "") //check to make sure the user has selected a file to import
            { //essentially, this will allow for the operation to change the names live
                Import_Button_Click(sender, e); //redo the import operation with the new information
            }
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e) //when the user clicks the select all button
        {
            Export_data.SelectAll(); //selects all sensors in the export list... pretty complicated right?
        }

        private int Interpolate_Array(int[] Arraytime, int[] Arraydata, int NewTime, int PlotMode)
        {
            int interpolated_int = 0;
            bool flag = false;
            int[] NewArray = new int[Arraytime.Length + 1];
            /*
            if (PlotMode == 2)
            {
                if (Arraydata.Length / Arraytime.Length > 1) //Check if there is more than one stream
                {
                    int ratio = Arraydata.Length / Arraytime.Length; //this variable is used to calculate which datapoints belong to which timestamp
                    for (int data_stream_itr = 0; data_stream_itr < ratio - 1; data_stream_itr++) //iterate over the number of data streams
                    {
                        for (int x_count = 1; x_count < Arraytime.Length + 1; x_count++) //iterate over the entire x array
                        {
                            try
                            {
                                NewArray[x_count - 1] = Arraydata[x_count * ratio + (data_stream_itr - (ratio - 2))];
                            }
                            catch (IndexOutOfRangeException)
                            {
                                break;
                            }
                        }
                    }
                }
                else
                {
                    for (int x_itr = 0; x_itr < Arraytime.Length - 1; x_itr++) //iterate over the entire x_axis
                    {
                        NewArray[x_itr] = Arraydata[x_itr];
                    }
                }
            }
            else
            { //In this section, each sensor produces one line series
                if (Arraytime.Length < Arraydata.Length) //Check if there is more than one stream
                {
                    int ratio = Arraydata.Length / Arraytime.Length; //used to find where the data streams end
                    int y_sum = 0; //These calculations need to be accumulated into one variable
                    string y_sum_string = ""; //some cases this is more helpful than using an int
                    for (int data_itr = 1; data_itr < Arraydata.Length + 1; data_itr++) //iterate over the entire data set
                    {
                        try
                        {
                            for (int data_stream_itr = 0; data_stream_itr < ratio; data_stream_itr++) //iterate over each data stream
                            {
                                if (PlotMode == 1) //the user selected "Concatenate streams DEC"
                                {
                                    y_sum_string = (Arraydata[data_itr * ratio - data_stream_itr - 1].ToString() + y_sum_string); //concatenates the data stream
                                }
                                else if (PlotMode == 3) //the user selected "add the streams"
                                {
                                    y_sum = y_sum + (int)Arraydata[data_itr * ratio - data_stream_itr - 1]; //Adds the data stream
                                }
                                else if (PlotMode == 4) //the user selected "subtract the streams"
                                {
                                    y_sum = y_sum - (int)Arraydata[data_itr * ratio - data_stream_itr - 1]; //Subtracts the data stream
                                }
                                else if (PlotMode == 5) //the user selected "Concatenate streams BIN"
                                {
                                    Error er = new Error("Fatal Error", "Choose a different Hex Handler! BIN not \r supported for interpolation"); //Can't get the thing to work so i got rid of it :)
                                    er.Show();
                                }
                            }
                            Int64 conversion_int = 0; //Just a variable to convert the strings to ints
                            if (PlotMode == 5) //if the number is in binary, we need to convert that before plotting
                            {
                                conversion_int = ConvertClass.Convert(y_sum_string); //convert to decimal
                            }
                            else if (PlotMode == 1)
                            {
                                conversion_int = Int64.Parse(y_sum_string); // change the string to int
                            }
                            else
                            {
                                conversion_int = y_sum; //set the variable to the int
                            }
                            NewArray[data_itr] = (int)conversion_int;
                            y_sum = 0; //reset the variables 
                            y_sum_string = "";
                        }
                        catch (IndexOutOfRangeException)
                        {
                            break;
                        }
                    }
                }
            }
            */
            for (int itr = 0; itr < Arraytime.Length; itr++)
            {
                if (Arraytime[itr] == NewTime)
                {
                    interpolated_int = NewArray[itr];
                    flag = true;
                    return interpolated_int;
                }
                if(Arraytime[itr] > NewTime && itr != 0)
                {
                    interpolated_int = (NewArray[itr] - NewArray[itr - 1]) / 2;
                    flag = true;
                    return interpolated_int;
                }
            }
            if (!flag)
            {
                interpolated_int = (NewArray[Arraydata.Length-1] - NewArray[Arraydata.Length - 2]) / 2 + NewArray[Arraydata.Length-1];
                return interpolated_int;
            }
            if (NewTime < Arraytime[0])
            {
                interpolated_int = (NewArray[0] - NewArray[01]) / 2 - NewArray[0];
                return interpolated_int;
            }
            return interpolated_int;
        }
    }
    public class MainViewModel //This class houses the plot modeling system
    {
        public static PlotModel mdl { get; set; }
        public MainViewModel()
        {
            this.MyModel = new PlotModel { Title = "Example 1" }; //just some sample text
            this.MyModel = mdl;
        }
        public string Title { get; private set; }
        public IList<DataPoint> Points { get; private set; } //allows for easy setting of datapoints in program
        public PlotModel MyModel { get; private set; }
    }

    public class DataVault //Global variables
    {
        //These variables can be accessed everywhere in the program
        public static string[] SensorNames { get; set; } //List of sensor names
        public static string[] ModuleNames { get; set; } //list of module names
        public static string PlotRequestX { get; set; } //requested x axis variable (always time)
        public static string[] PlotRequestY { get; set; } //requested y axis variables
        public static string[] RawData { get; set; } //raw data from the original data file
        public static string[] RawTime { get; set; } //raw timestamp information from the orginal data file
        public static int Number_of_y_plots { get; set; } //global counter for how many y-axis are requested
        public static string[] LineColors { get; set; } //user selected colors for the plot lines
        public static bool[] DatapointDots { get; set; } //shows datapoints on plots
        public static LineStyle[] LineStyle { get; set; } //user selected line styles
        public static string[] LineNames { get; set; } //user defined line names
        public static int CallBackID { get; set; } //Special int that checks which edit button is being pressed in the plot setup menu
        public static string TempString { get; set; } //temporary global variable used to check names
        public static bool PlotSetup { get; set; } //checks if there has been a plot setup
        public static string[] LinePlotMode { get; set; } //user defined how to handle data streams
        public static bool IncludeHex { get; set; } //show hex IDs with each enumerated name
        public static bool AllowInterpolation { get; set; } //Allows the user to select anything except time for x-axis
    }

    public static class ConvertClass //simple class used to convert ints to binary
    {
        public static int Convert(string str1)
        {
            if (str1 == "")
                throw new Exception("Invalid input");
            int val = 0, res = 0;

            for (int i = 0; i < str1.Length; i++)
            {
                try
                {
                    val = Int32.Parse(str1[i].ToString());
                    if (val == 1)
                        res += (int)Math.Pow(2, str1.Length - 1 - i);
                    else if (val > 1)
                        throw new Exception("Invalid!");
                }
                catch
                {
                    throw new Exception("Invalid!");
                }
            }
            return res;
        }
    }

    public static class StringExt //Adds new function to strings
    {
        public static string Truncate(this string value, int maxLength) //truncates string by maxLength
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}