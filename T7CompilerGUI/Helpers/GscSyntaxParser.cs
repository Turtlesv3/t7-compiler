using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace T7CompilerGUI.Helpers
{
    /// <summary>
    /// Parses GSC.xshd syntax definition file and extracts keywords, colors, and rules
    /// for use with ScintillaNET syntax highlighting
    /// </summary>
    public class GscSyntaxParser
    {
        public class GscSyntaxData
        {
            public Dictionary<string, Color> Colors { get; set; } = new Dictionary<string, Color>();
            public List<string> Keywords { get; set; } = new List<string>();
            public List<string> CommandKeywords { get; set; } = new List<string>();
            public List<string> PreprocessorKeywords { get; set; } = new List<string>();
            public List<string> BuiltInFunctions { get; set; } = new List<string>();
            public List<string> TrueFalse { get; set; } = new List<string>();
        }

        /// <summary>
        /// Parses the GSC.xshd file and extracts all syntax highlighting data
        /// </summary>
        public static GscSyntaxData ParseGscXshd(string xshdFilePath)
        {
            if (!File.Exists(xshdFilePath))
                throw new FileNotFoundException($"GSC.xshd file not found: {xshdFilePath}");

            var data = new GscSyntaxData();
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(xshdFilePath);

            // Parse colors
            var colorNodes = xmlDoc.SelectNodes("//Color");
            if (colorNodes != null)
            {
                foreach (XmlNode colorNode in colorNodes)
                {
                    string name = colorNode.Attributes["name"]?.Value;
                    string foreground = colorNode.Attributes["foreground"]?.Value;
                    
                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(foreground))
                    {
                        Color color = ParseColor(foreground);
                        data.Colors[name] = color;
                    }
                }
            }

            // Parse keywords by category
            var keywordNodes = xmlDoc.SelectNodes("//Keywords");
            if (keywordNodes != null)
            {
                foreach (XmlNode keywordNode in keywordNodes)
                {
                    string colorAttr = keywordNode.Attributes["color"]?.Value;
                    var wordNodes = keywordNode.SelectNodes("Word");
                    
                    if (wordNodes != null)
                    {
                        foreach (XmlNode wordNode in wordNodes)
                        {
                            string word = wordNode.InnerText?.Trim();
                            if (string.IsNullOrEmpty(word))
                                continue;

                            // Add to appropriate list based on color attribute
                            switch (colorAttr)
                            {
                                case "Keywords":
                                    if (!data.Keywords.Contains(word))
                                        data.Keywords.Add(word);
                                    break;
                                case "CommandKeywords":
                                    if (!data.CommandKeywords.Contains(word))
                                        data.CommandKeywords.Add(word);
                                    break;
                                case "PreprocessorKeywords":
                                    if (!data.PreprocessorKeywords.Contains(word))
                                        data.PreprocessorKeywords.Add(word);
                                    break;
                                case "BuiltInFunctions":
                                    if (!data.BuiltInFunctions.Contains(word))
                                        data.BuiltInFunctions.Add(word);
                                    break;
                                case "TrueFalse":
                                    if (!data.TrueFalse.Contains(word))
                                        data.TrueFalse.Add(word);
                                    break;
                            }
                        }
                    }
                }
            }

            return data;
        }

        /// <summary>
        /// Parses a color string (hex format like "#4DA642" or named colors)
        /// </summary>
        private static Color ParseColor(string colorString)
        {
            if (string.IsNullOrEmpty(colorString))
                return Color.Black;

            // Handle hex colors
            if (colorString.StartsWith("#"))
            {
                try
                {
                    // Remove # and parse
                    string hex = colorString.Substring(1);
                    if (hex.Length == 6)
                    {
                        int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                        int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                        int b = Convert.ToInt32(hex.Substring(4, 2), 16);
                        return Color.FromArgb(r, g, b);
                    }
                }
                catch
                {
                    // Fall through to named color parsing
                }
            }

            // Handle named colors
            try
            {
                return Color.FromName(colorString);
            }
            catch
            {
                return Color.Black;
            }
        }

        /// <summary>
        /// Gets the default GSC.xshd file path (in the T7-Compiler-UI-main project)
        /// </summary>
        public static string GetDefaultGscXshdPath()
        {
            // Try to find GSC.xshd in the T7-Compiler-UI-main project
            string[] possiblePaths = new[]
            {
                @"C:\Users\HP\Desktop\ogs\T7-Compiler-UI-main\Idea\GSC.xshd",
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GSC.xshd"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "T7-Compiler-UI-main", "Idea", "GSC.xshd")
            };

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                    return path;
            }

            return null;
        }
    }
}

