using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Dan200.Core.RSS
{
    public class RSSFile
    {
        public readonly IList<RSSChannel> Channels;

        public RSSFile()
        {
            Channels = new List<RSSChannel>();
        }

        public RSSFile(Stream stream) : this()
        {
            // Read document
            var document = new XmlDocument();
            try
            {
                document.Load(stream);
            }
            catch (Exception)
            {
                return;
            }

            // Parse document
            var root = document.DocumentElement;
            if (root.Name == "rss")
            {
                var rss = root;
                var channelItems = rss.GetElementsByTagName("channel").OfType<XmlElement>();
                foreach (var channelItem in channelItems)
                {
                    var channel = new RSSChannel();

                    // Parse channel info
                    {
                        var title = channelItem["title"];
                        if (title != null)
                        {
                            channel.Title = title.InnerText;
                        }
                        var description = channelItem["description"];
                        if (description != null)
                        {
                            channel.Description = description.InnerText;
                        }
                        var link = channelItem["link"];
                        if (link != null)
                        {
                            channel.Link = link.InnerText;
                        }
                    }

                    // Parse entries
                    {
                        var entryItems = channelItem.GetElementsByTagName("item").OfType<XmlNode>();
                        foreach (var entryItem in entryItems)
                        {
                            var entry = new RSSEntry();

                            // Parse entry info
                            var title = entryItem["title"];
                            if (title != null)
                            {
                                entry.Title = title.InnerText;
                            }
                            var description = entryItem["description"];
                            if (description != null)
                            {
                                entry.Description = description.InnerText;
                            }
                            var link = entryItem["link"];
                            if (link != null)
                            {
                                entry.Link = link.InnerText;
                            }

                            // Store entry
                            channel.Entries.Add(entry);
                        }
                    }

                    // Store channel
                    Channels.Add(channel);
                }
            }
        }

        public RSSChannel GetChannel(string title)
        {
            for (int i = 0; i < Channels.Count; ++i)
            {
                if (Channels[i].Title == title)
                {
                    return Channels[i];
                }
            }
            return null;
        }
    }
}

