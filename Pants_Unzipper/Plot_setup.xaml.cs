using System.Collections.Generic;
using System.Windows;

namespace Pants_Unzipper
{
    /// <summary>
    /// Interaction logic for Plot_setup.xaml
    /// </summary>
    public partial class Plot_setup : Window
    {
        public Plot_setup() //general plot setup window
        {
            InitializeComponent(); //start components
            //When there are multiple y-axis requested, the size of the window needs to increase
            this.Height = this.Height + 67*DataVault.Number_of_y_plots; //increase the window if there are more than one
            Thickness margin = X_label.Margin; //get the x-label's location
            margin.Top = X_label.Margin.Top + 67 * DataVault.Number_of_y_plots; //move the label if there are more than one plot requested
            X_label.Margin = margin; //apply the move to the label
            margin.Top = SensorList_x.Margin.Top + 67 * DataVault.Number_of_y_plots; //move the x-sensor list same amount
            SensorList_x.Margin = margin; //apply the move
            if (DataVault.Number_of_y_plots == 1) //turn on the other labels and sensorlists
            {
                Y_label_2.Visibility = Visibility.Visible;
                SensorList_y_2.Visibility = Visibility.Visible;
                //we only want to show the bottom-most X button
                X_2.Visibility = Visibility.Hidden; 
                X_1.Visibility = Visibility.Visible;
                Edit_2.Visibility = Visibility.Visible;
                Edit_3.Visibility = Visibility.Hidden;
                Edit_4.Visibility = Visibility.Hidden;
            }
            if(DataVault.Number_of_y_plots == 2) //same as above, but for another plot
            {
                Y_label_2.Visibility = Visibility.Visible;
                SensorList_y_2.Visibility = Visibility.Visible;
                Y_label_3.Visibility = Visibility.Visible;
                SensorList_y_3.Visibility = Visibility.Visible;
                X_1.Visibility = Visibility.Hidden;
                X_2.Visibility = Visibility.Visible;
                Edit_2.Visibility = Visibility.Visible;
                Edit_3.Visibility = Visibility.Visible;
                Edit_4.Visibility = Visibility.Hidden;
            }
            SensorList_x.SelectedItem = DataVault.PlotRequestX; //select the default x-axis
            try
            {
                SensorList_y.SelectedItem = DataVault.PlotRequestY[0]; //show the default (or previously selected) sensors
                SensorList_y_2.SelectedItem = DataVault.PlotRequestY[1];
                SensorList_y_3.SelectedItem = DataVault.PlotRequestY[2];
                SensorList_x.SelectedItem = DataVault.PlotRequestX;
            }
            catch
            {}
            List<string> SensorList = new List<string>(); //make a new list for the sensors
            List<string> SensorList_exp = new List<string>(); // for the x-axis
            SensorList_exp.Add("Time"); //the time as an option in x-axis dropdown
            for (int Sensor_itr = 0; Sensor_itr < DataVault.SensorNames.Length; Sensor_itr++) //iterate over all sensor names
            {
                SensorList.Add(DataVault.SensorNames[Sensor_itr]); //add the sensor name to the list
                if (DataVault.AllowInterpolation)
                {
                    SensorList_exp.Add(DataVault.SensorNames[Sensor_itr]);
                }
                else
                {
                    SensorList_x.SelectedIndex = 0;
                }
            }
            SensorList_x.ItemsSource = SensorList_exp; //apply the lists to the dropdown menus
            SensorList_y.ItemsSource = SensorList;
            SensorList_y_2.ItemsSource = SensorList;
            SensorList_y_3.ItemsSource = SensorList;
        }

        private void Plot_selected_Click(object sender, RoutedEventArgs e) //When the user clicks the apply butotn
        {
            DataVault.PlotRequestX = SensorList_x.SelectedItem.ToString(); //save the selected x-axis
            if (DataVault.Number_of_y_plots == 0 && SensorList_y.SelectedItem != null) //if something has been seleced and there is only one plot
            {
                string[] Requested_Ys = { SensorList_y.SelectedItem.ToString() }; //save the selected sensor
                DataVault.PlotRequestY = Requested_Ys; 
                DataVault.PlotSetup = true; //plot has been setup, set flag

            }
            else if (DataVault.Number_of_y_plots == 1 && SensorList_y.SelectedItem != null && SensorList_y_2.SelectedItem != null) //same as above, but for two plots
            {
                string[] Requested_Ys = { SensorList_y.SelectedItem.ToString(), SensorList_y_2.SelectedItem.ToString() };
                DataVault.PlotRequestY = Requested_Ys;
                DataVault.PlotSetup = true;
            }
            else if(SensorList_y.SelectedItem != null && SensorList_y_2.SelectedItem != null && SensorList_y_3.SelectedItem != null) //same as above, but for all three plots
            {
                string[] Requested_Ys = { SensorList_y.SelectedItem.ToString(), SensorList_y_2.SelectedItem.ToString(), SensorList_y_3.SelectedItem.ToString() };
                DataVault.PlotRequestY = Requested_Ys;
                DataVault.PlotSetup = true;
            }
            else
            {
                DataVault.PlotSetup = false;
            }
            this.Close(); //close the window
        }

        private void Add_y_button_Click(object sender, RoutedEventArgs e) //When the user clicks the add another y button
        {
            if (DataVault.Number_of_y_plots <= 1) //checks if there are not 3 plots already displaying
            {
                this.Height = this.Height + 67; //increase the height of the window
                Thickness margin = X_label.Margin; //get margin information for the x-label
                margin.Top = X_label.Margin.Top + 67; //move the label down
                X_label.Margin = margin; //applt the change
                margin.Top = SensorList_x.Margin.Top + 67;//move the x-dropdown down
                SensorList_x.Margin = margin; //apply the change

                if (DataVault.Number_of_y_plots == 0) //if this is the first plot, some elements need to be hidden
                {
                    Y_label_2.Visibility = Visibility.Visible;
                    SensorList_y_2.Visibility = Visibility.Visible;
                    //we only want one x button available at any time
                    X_2.Visibility = Visibility.Hidden;
                    X_1.Visibility = Visibility.Visible;
                    Edit_2.Visibility = Visibility.Visible;
                    Edit_3.Visibility = Visibility.Hidden;
                    Edit_4.Visibility = Visibility.Hidden;
                }
                else //must be the second plot, same as above
                {
                    Y_label_3.Visibility = Visibility.Visible;
                    SensorList_y_3.Visibility = Visibility.Visible;
                    X_1.Visibility = Visibility.Hidden;
                    X_2.Visibility = Visibility.Visible;
                    Edit_2.Visibility = Visibility.Visible;
                    Edit_3.Visibility = Visibility.Visible;
                    Edit_4.Visibility = Visibility.Hidden;
                }
                DataVault.Number_of_y_plots++; //increase the plot count
            }
            if(DataVault.Number_of_y_plots >= 2) //checks if there are three plots already being displayed
            {
                Add_y_button.IsEnabled = false; //don't let the user click the button if at max number of plots
            }
        }

        private void X_1_Click(object sender, RoutedEventArgs e) //When the user clicks on one of the x buttons
        {
            //Just like the setup (and adding another y function), we are reducing the size of the window and hiding some elements
            this.Height = this.Height - 67;
            Add_y_button.IsEnabled = true;
            Thickness margin = X_label.Margin;
            margin.Top = X_label.Margin.Top - 67;
            X_label.Margin = margin;
            margin.Top = SensorList_x.Margin.Top - 67;
            SensorList_x.Margin = margin;
            Y_label_2.Visibility = Visibility.Hidden;
            SensorList_y_2.Visibility = Visibility.Hidden;
            X_2.Visibility = Visibility.Hidden;
            X_1.Visibility = Visibility.Hidden;
            Edit_2.Visibility = Visibility.Hidden;
            Edit_3.Visibility = Visibility.Hidden;
            Edit_4.Visibility = Visibility.Hidden;
            DataVault.Number_of_y_plots--; //reduce the number of plots
        }

        private void X_2_Click(object sender, RoutedEventArgs e) //When the user clicks on one of the x buttons
        {
            //see x_1_click
            Add_y_button.IsEnabled = true;
            this.Height = this.Height - 67;
            Thickness margin = X_label.Margin;
            margin.Top = X_label.Margin.Top - 67;
            X_label.Margin = margin;
            margin.Top = SensorList_x.Margin.Top - 67;
            SensorList_x.Margin = margin;
            Y_label_3.Visibility = Visibility.Hidden;
            SensorList_y_3.Visibility = Visibility.Hidden;
            X_1.Visibility = Visibility.Visible;
            X_2.Visibility = Visibility.Hidden;
            Edit_2.Visibility = Visibility.Visible;
            Edit_3.Visibility = Visibility.Hidden;
            Edit_4.Visibility = Visibility.Hidden;
            DataVault.Number_of_y_plots--;
        }

        private void Edit_1_Click(object sender, RoutedEventArgs e) //edit the first y axis
        {
            try
            {
                DataVault.TempString = SensorList_y.SelectedItem.ToString(); //save the sensor name
                Variable_setup st = new Variable_setup(1); //open variable setup (the number 1 is to tell the function this is the first y-axis)
                st.Show(); //show the new window
            }
            catch
            {
                Error err = new Error("No Sensor Selected", "Please Selected a Sensor from the drop down menu before clicking."); //error handling
                err.Show();
            }
        }

        private void Edit_2_Click(object sender, RoutedEventArgs e) //edit the second y axis
        {
            try
            {
                DataVault.TempString = SensorList_y_2.SelectedItem.ToString(); //save the name of the sensor
                Variable_setup st = new Variable_setup(2); //open variable setup (the number 2 is to tell the function this is the second y axis
                st.Show(); //show the new window
            }
            catch
            {
                Error err = new Error("No Sensor Selected", "Please Selected a Sensor from the drop down menu before clicking."); //error handling
                err.Show();
            }
        }

        private void Edit_3_Click(object sender, RoutedEventArgs e) //edit the third y axis
        {
            try
            {
                DataVault.TempString = SensorList_y_3.SelectedItem.ToString(); //save the name of the sensor
                Variable_setup st = new Variable_setup(3); //open variable setup (the number 3 is to tell the function this is the third y axis
                st.Show(); //show the new window
            }
            catch
            {
                Error err = new Error("No Sensor Selected", "Please Selected a Sensor from the drop down menu before clicking."); //error handling
                err.Show();
            }
        }

        private void Edit_4_Click(object sender, RoutedEventArgs e) //not used :O
        {
            Variable_setup st = new Variable_setup(4); //not actually used :(
            st.Show();
        }

    }
}
