//
//   Copyright 2011 Igor Ralic

//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at

//       http://www.apache.org/licenses/LICENSE-2.0

//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//


using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using Coding4Fun.Phone.Controls;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.Controls;

namespace System
{
    public static class ReviewBugger
    {
        private const int numOfRunsBeforeFeedback = 3;
        private static int numOfRuns = -1;
        private static readonly IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
        private static readonly Button yesButton = new Button() { Content = "Yes", Width = 120 };
        private static readonly Button laterButton = new Button() { Content = "Later", Width = 120 };
        private static readonly Button neverButton = new Button() { Content = "Never", Width = 120 };
        private static readonly MessagePrompt messagePrompt = new MessagePrompt();

        public static void CheckNumOfRuns()
        {
            if (!settings.Contains("numOfRuns"))
            {
                numOfRuns = 1;
                settings.Add("numOfRuns", 1);
            }
            else if (settings.Contains("numOfRuns") && (int)settings["numOfRuns"] != -1)
            {
                settings.TryGetValue("numOfRuns", out numOfRuns);
                numOfRuns++;
                settings["numOfRuns"] = numOfRuns;
            }
        }

        public static void DidReview()
        {
            if (settings.Contains("numOfRuns"))
            {
                numOfRuns = -1;
                settings["numOfRuns"] = -1;
            }
        }

        public static void NotNow()
        {
            numOfRuns = -1;
        }

        public static bool IsTimeForReview()
        {
            return numOfRuns % numOfRunsBeforeFeedback == 0 ? true : false;
        }

        static void yesButton_Click(object sender, RoutedEventArgs e)
        {
            var review = new MarketplaceReviewTask();
            review.Show();
            messagePrompt.Hide();
            DidReview();
        }

        static void laterButton_Click(object sender, RoutedEventArgs e)
        {
            numOfRuns = -1;
            messagePrompt.Hide();
        }

        static void neverButton_Click(object sender, RoutedEventArgs e)
        {
            DidReview();
            messagePrompt.Hide();
        }
    }
}
