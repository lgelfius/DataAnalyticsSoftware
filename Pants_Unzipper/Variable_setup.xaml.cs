using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using LegalNameCheck;

namespace Pants_Unzipper
{
    /// <summary>
    /// Interaction logic for Variable_setup.xaml
    /// </summary>
    public partial class Variable_setup : Window
    {
        string NewName = ""; //empty variable for a new name
        string Name_c = ""; //checked string
        bool NeedsRecount = false; //some cases need recount
        LegalName GLegal = new LegalName(); //generates a legal namecheck

        public Variable_setup(int id) //We need to know which one of the three y-axis is requesting this page. The ID keeps track of this
        {
            InitializeComponent(); //start components on the screen
            List<string> HexHandle = new List<string>(); //set user dropdown menu with options for how to handle the data streams
            HexHandle.Add("Concatenate All Streams (Dec)");
            HexHandle.Add("Concatenate All Streams (Bin)");
            HexHandle.Add("Show All Streams");
            HexHandle.Add("Add All Streams");
            HexHandle.Add("Subtract All Streams");
            HexHandler.ItemsSource = HexHandle; //apply list to the dropdown menu
            List<string> ColorList = new List<string>(); //set user dropdown menu with legal colors for the lines
            ColorList.Add("Auto");
            ColorList.Add("Red");
            ColorList.Add("Blue");
            ColorList.Add("Green");
            ColorList.Add("Orange");
            ColorList.Add("Yellow");
            ColorList.Add("Indigo");
            ColorList.Add("Voilet");
            ColorList.Add("Black");
            ColorList.Add("White");
            Color_List.ItemsSource = ColorList; // apply list to the dropdown menu
            List<string> LineStyleList = new List<string>(); //set user dropdown menu with legal line styles
            LineStyleList.Add("Dash");
            LineStyleList.Add("Long Dash");
            LineStyleList.Add("Dot");
            LineStyleList.Add("Solid");
            LineStyleList.Add("None");
            LineStyle_select.ItemsSource = LineStyleList; //apply list to the dropdown menu

            if (DataVault.LineNames[id-1] == null) //check if there is a defined name for this sensor
            {
                Custom_Name.Text = DataVault.TempString; //if not, set the name box to the temporary string (which is the sensor name)
            }
            else
            {
                Custom_Name.Text = DataVault.LineNames[id - 1]; //set the name to the one defined in the datavault
            }
            Color_List.SelectedItem = DataVault.LineColors[id - 1]; //display default (or previously selected) linecolor
            HexHandler.SelectedItem = DataVault.LinePlotMode[id - 1]; //display default (or previously selected) ploting mode
            LineStyle_select.SelectedItem = DataVault.LineStyle[id - 1].ToString();//display default (or previously selected) line style
            datapointHighlight.IsChecked = DataVault.DatapointDots[id - 1]; ////display default (or previously selected) highlight datapoints option
            DataVault.CallBackID = id; //save the call ID for use later
        }

        private void Custom_Name_TextChanged(object sender, TextChangedEventArgs e) //if there is a change to the custom name field
        {
            LegalName lgname = new LegalName(); //make a new instance of the legalname
            bool isLegal = lgname.Check(Custom_Name.Text); //check if the name is legal
            if(!isLegal)
            {
                Ess.Visibility = Visibility.Visible; //display warning
            }
            else
            {
                NewName = Custom_Name.Text; //save the new name for storage
                try
                {
                    Ess.Visibility = Visibility.Hidden; //hide warning
                }
                catch
                { }
            }
            Name_c = lgname.CheckLn(Custom_Name.Text); 
            NeedsRecount = lgname.Recount("Al",lgname.IsFastMode(Custom_Name.Text)); //determine if a recount is needed
        }

        private void Apply_Button_Click(object sender, RoutedEventArgs e) //When the user clicks the apply button
        {
            DataVault.LineColors[DataVault.CallBackID-1] = Color_List.SelectedValue.ToString(); //save the color to the DataVault
            DataVault.LinePlotMode[DataVault.CallBackID - 1] = HexHandler.SelectedValue.ToString(); //save the data stream settings to the DataVault
            OxyPlot.LineStyle transfer_style = new OxyPlot.LineStyle(); //make blank line style
            switch(LineStyle_select.SelectedItem.ToString()) //change the linestyle string selected to an actual linestyle
            {
                case ("Dash"):
                    transfer_style = OxyPlot.LineStyle.Dash;
                    break;
                case ("Dot"):
                    transfer_style = OxyPlot.LineStyle.Dot;
                    break;
                case ("Solid"):
                    transfer_style = OxyPlot.LineStyle.Solid;
                    break;
                case ("Long Dash"):
                    transfer_style = OxyPlot.LineStyle.LongDash;
                    break;
                default:
                    transfer_style = OxyPlot.LineStyle.None;
                    break;
            }
            DataVault.LineStyle[DataVault.CallBackID - 1] = transfer_style; //save the line style to the DataVault
            GLegal.Recount("Es", NeedsRecount); //Recount
            DataVault.DatapointDots[DataVault.CallBackID - 1] = datapointHighlight.IsChecked.Value; //save the datapoint highlighting option to the DataVault
            if (NewName != "") //checks if there is nothing in the name tag
            { 
                DataVault.LineNames[DataVault.CallBackID - 1] = NewName; //save the name entered to the DataVault
            }
            else
            {
                DataVault.LineNames[DataVault.CallBackID - 1] = DataVault.TempString; //save the sensor name to the DataVault
            }
            if(Name_c != "") //check if name is blank
            {
                Error err = new Error("Error", Name_c); //generate new error message
                err.Show(); //show message
                DataVault.LineNames[DataVault.CallBackID - 1] = Name_c; //Save name to the DataVault
            }
            this.Close(); //close window
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) //When the cancel button is set
        {
            GLegal.Recount("Ch", NeedsRecount); //Recount
            this.Close(); //close window
        }

        private void DatapointHighlight_Click(object sender, RoutedEventArgs e) //if the checkbox is changed
        {
            DataVault.DatapointDots[DataVault.CallBackID - 1] = datapointHighlight.IsChecked.Value; //save the state of the checkbox. this changes if datapoints will be shown on the graph
        }
    }
}
