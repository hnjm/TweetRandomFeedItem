using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Rss;
using Tweetinvi;

namespace TweetRandomFeedItem
{
    class Program
    {
        const string DEFAULT_MAX_TAG_COUNT = "3";
        const int TWITTER_URL_SIZE = 23;
        const int MAX_TWEET_LENGTH = 280;

        static Random rnd = new Random();

        public static void Main()
        {
            var message = CreateMessageFromRandomFeedItem();

            SendTweet(message);

            Console.WriteLine($"Tweeted message:\r\n\r\n{message}");
        }

        static string CreateMessageFromRandomFeedItem()
        {
            var ghostUri = Helper.GetEnv("RSS_URI");
            var maxTagCount = Convert.ToInt32(Helper.GetEnv("TWITTER_MAX_TAG_COUNT", DEFAULT_MAX_TAG_COUNT));
            var maxItemsToRead = Convert.ToInt32(Helper.GetEnv("RSS_ITEM_RETRIEVAL_LIMIT", Int32.MaxValue.ToString()));

            var random = new Random();
            var pattern = new Regex("[- ]");
            var entries = new List<ISyndicationItem>();

            using (var xmlReader = XmlReader.Create(ghostUri))
            {
                var feedReader = new RssFeedReader(xmlReader);

                var itemsRead = 0;
                while (feedReader.Read().Result && itemsRead < maxItemsToRead)
                {
                    if (feedReader.ElementType == SyndicationElementType.Item)
                    {
                        entries.Add(feedReader.ReadItem().Result);
                        itemsRead++;
                    }
                }
            }

            var selectedEntry = entries[random.Next(0, entries.Count - 1)];

            var remainingSpaceForTags = MAX_TWEET_LENGTH - (selectedEntry.Title.Length + 1 + TWITTER_URL_SIZE);

            var tags = selectedEntry.Categories.Take(maxTagCount).Select(x => $"#{pattern.Replace(x.Name, "_").Replace("#", "sharp").Replace(".", "dot")}");

            var tagsFlat = "";
            foreach (var t in tags)
            {
                if ($"{tagsFlat} {t}".Length > remainingSpaceForTags)
                    break;

                tagsFlat += $" {t}";
            }

            return $"{selectedEntry.Title}{tagsFlat}\r\n{selectedEntry.Links.ToList()[0].Uri}";
        }

        static void SendTweet(string message)
        {
            var consumerKey = Helper.GetEnv("TWITTER_CONSUMER_KEY");
            var consumerSecret = Helper.GetEnv("TWITTER_CONSUMER_SECRET");
            var userAccessToken = Helper.GetEnv("TWITTER_USER_ACCESS_TOKEN");
            var userAccessTokenSecret = Helper.GetEnv("TWITTER_USER_ACCESS_TOKEN_SECRET");

            Auth.SetUserCredentials(consumerKey, consumerSecret, userAccessToken, userAccessTokenSecret);

            Tweet.PublishTweet(message);
        }
    }
}
