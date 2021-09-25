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
            //set date label
            Date.Content = DateTime.Now.ToString("dd/MM/yy HH:mm");

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
                }
                
            }
            
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

        private void CalendarButton_Click(object sender, RoutedEventArgs e)
        {
            FillDates();
            Calendar calendar = new Calendar();
            calendar.Show();
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

        private void GoalAchievedButton_Click(object sender, RoutedEventArgs e)
        {
            FillDates();
            GoalAchieved(DateTime.Now);
        }

        private int GetIndexFromDate(DateTime date)
        {
            //get date without time
            date = new DateTime(date.Year, date.Month, date.Day);
            //get index of date in list
            int index = dateList.FindIndex((i) => i.dateTime == date);
            return index;
        }

        public bool GoalAchieved(DateTime date)
        {
            int index = GetIndexFromDate(date);
            if(index > -1 && dateList[index].isGoalReached == false)
            {
                //set bool to true of goal reached
                Date curDate = dateList[index];
                curDate.isGoalReached = true;
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
                    //add date to list
                    DateTime now = new DateTime(fromDate.Year, fromDate.Month, fromDate.Day);
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
