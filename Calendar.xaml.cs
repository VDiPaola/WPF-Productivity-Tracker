using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ProductivityTracker
{
    /// <summary>
    /// Interaction logic for Calendar.xaml
    /// </summary>
    public partial class Calendar : Window
    {

        private MainWindow mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
        System.Windows.Controls.Calendar calendar = new System.Windows.Controls.Calendar();
        public Calendar()
        {
            InitializeComponent();

            List<Date> dateList = mainWindow.dateList;

            //initialise calendar component
            calendar.IsTodayHighlighted = false;
            calendar.SelectedDatesChanged += DateSelect;

            //get list of dates that have goal achieved, formatted for the calendar
            List<CalendarDateRange> blackOutDates = dateList.Where(date => date.isGoalReached).Select(date => new CalendarDateRange(date.dateTime)).ToList();
            //add each blackout date to calendar
            foreach(CalendarDateRange range in blackOutDates)
            {
                calendar.BlackoutDates.Add(range);
            }


            //make calender the child of view box
            CalendarContainer.Child = calendar;
        }

        private void DateSelect(object sender, SelectionChangedEventArgs e)
        {
            DateTime selectedDate = (DateTime)e.AddedItems[0];
            bool dateExists = mainWindow.GoalAchieved(selectedDate);
            if (dateExists)
            {
                calendar.BlackoutDates.Add(new CalendarDateRange(selectedDate));
            }
        }
    }
}
