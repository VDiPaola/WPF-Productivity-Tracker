using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProductivityTracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DateTime timer;
        private bool hasTimerStarted = false;
        private bool isTimerPaused = false;
        private System.Timers.Timer asyncTimer;

        public readonly string path = "./datelist.json";

        public List<Date> dateList = new List<Date>();


        public MainWindow()
        {
            InitializeComponent();
            

            //create timer
            asyncTimer = new System.Timers.Timer(1000);
            asyncTimer.Elapsed += OnElapsed;
            asyncTimer.Enabled = false;

            ResetTimer();

            //create datelist file if doesnt exist
            File.AppendText(path).Close();
            //import datelist
            using (StreamReader sr = new StreamReader(path))
            {
                List<Date> imported = JsonConvert.DeserializeObject<List<Date>>(sr.ReadToEnd());
                if (imported != null) {
                    dateList = imported;
                    dateList.Sort((x, y) => x.dateTime.CompareTo(y.dateTime));
                }
                
            }

            //set date picker and productivity time
            Date.SelectedDate = DateTime.Now;
            UpdateDateStats(DateTime.Now);

            //mark dates that are achieved
            List<CalendarDateRange> blackOutDates = dateList.Where(date => date.isGoalReached).Select(date => new CalendarDateRange(date.dateTime)).ToList();
            foreach (CalendarDateRange range in blackOutDates)
            {
                //Date.BlackoutDates.Add(range);
            }

        }

        private void UpdateDateStats(DateTime date)
        {
            int index = GetIndexFromDate(date);
            if (index > -1)
            {
                ProductivityTime.Content = dateList[index].productivityTime.ToString("HH:mm:ss");
                GoalAchievedCheckbox.IsChecked = dateList[index].isGoalReached;
            }
            else
            {
                ProductivityTime.Content = "00:00:00";
                GoalAchievedCheckbox.IsChecked = false;
            }

        }

        private void DateChanged(object sender, SelectionChangedEventArgs e)
        {
            DateTime date = (DateTime)e.AddedItems[0];
            UpdateDateStats(date);
        }

        private void GoalAchievedCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            FillDates();
            SetGoalAchieved((DateTime)Date.SelectedDate, (bool)GoalAchievedCheckbox.IsChecked);
            
        }

        private void OnElapsed(Object source, ElapsedEventArgs e)
        {
            //update timer label
            Dispatcher.BeginInvoke(new ThreadStart(() =>{
                timer = timer.AddSeconds(1);
                UpdateTimer();
            }));
        }

        private void UpdateTimer()
        {
            Timer.Content = timer.ToString("HH:mm:ss");
        }

        private void StartTimer()
        {
            hasTimerStarted = true;
            asyncTimer.Start();

        }
        private void StopTimer()
        {
            asyncTimer.Stop();
        }

        private void ResetTimer()
        {
            StopTimer();
            hasTimerStarted = false;
            isTimerPaused = false;
            PauseButton.Content = "Pause";
            StartButton.Content = "Start";
            timer = new DateTime();
            UpdateTimer();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            //start or reset timer
            if (!hasTimerStarted)
            {
                StartTimer();
                (sender as Button).Content = "Reset";
            }
            else
            {
                ResetTimer();
                (sender as Button).Content = "Start";
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (hasTimerStarted)
            {
                //pause button to stop and start the timer
                isTimerPaused = !isTimerPaused;
                (sender as Button).Content = isTimerPaused ? "Resume" : "Pause";
                if (isTimerPaused) { StopTimer(); } else { StartTimer(); }
            }
  
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            FillDates();
            //add current timer to productivity time in date list
            Date curDate = dateList[dateList.Count - 1];
            curDate.productivityTime = curDate.productivityTime.Add(new TimeSpan(timer.Hour, timer.Minute, timer.Second));
            dateList[dateList.Count - 1] = curDate;

            ResetTimer();

            SaveDateList();

        }
        private void SaveCustomButton_Click(object sender, RoutedEventArgs e)
        {
            FillDates();
            //add custom timer to productivity time in date list
            Date curDate = dateList[dateList.Count - 1];
            DateTime customTimer = new DateTime().AddMinutes(CustomTimerSlider.Value);
            curDate.productivityTime = curDate.productivityTime.Add(new TimeSpan(customTimer.Hour, customTimer.Minute, customTimer.Second));
            dateList[dateList.Count - 1] = curDate;

            CustomTimerSlider.Value = 0;

            SaveDateList();

        }

        private void SaveDateList()
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(dateList));
        }

        private void GoalToggleButton_Click(object sender, RoutedEventArgs e)
        {
            FillDates();
            SetGoalAchieved(DateTime.Now);
        }

        private int GetIndexFromDate(DateTime date)
        {
            //get date without time
            date = new DateTime(date.Year, date.Month, date.Day);
            //get index of date in list
            int index = dateList.FindIndex((i) => i.dateTime == date);
            return index;
        }

        private bool SetGoalAchieved(DateTime date, bool achieved = true)
        {
            int index = GetIndexFromDate(date);
            if(index > -1)
            {
                //set bool to true of goal reached
                Date curDate = dateList[index];
                curDate.isGoalReached = achieved;
                dateList[index] = curDate;

                //save
                SaveDateList();

                return true;
            }

            return false;
        }

        private void FillDates()
        {
            //fills in with missing dates in list from last date to now
            if(dateList.Count > 0)
            {
                //from last date in array(+1) to now
                DateTime fromDate = (dateList[dateList.Count-1].dateTime).AddDays(1);
                DateTime toDate = DateTime.Now;
                TimeSpan days = toDate - fromDate;
                if(days.Days < 1) { return; }
                for (int i =0; i <= days.Days; i++)
                {
                    DateTime now = new DateTime(fromDate.Year, fromDate.Month, fromDate.Day);
                    //make sure date doesnt exist yet
                    int index = GetIndexFromDate(now);
                    if (index > -1) { return; }
                    //add date to list
                    Date date = new Date() { dateTime = now, isGoalReached = false, productivityTime = new DateTime() };
                    dateList.Add(date);
                    
                    //increment day
                    fromDate = fromDate.AddDays(1);
                 
                }
            }
            else
            {
                //add current Date object to list
                DateTime now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
                dateList.Add(new Date() { dateTime = now, isGoalReached = false, productivityTime = new DateTime() });
            }
            

        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            DateTime time = new DateTime().AddMinutes(e.NewValue);
            CustomTimer.Content = time.ToString("HH:mm:ss");
        }

        
    }
}
