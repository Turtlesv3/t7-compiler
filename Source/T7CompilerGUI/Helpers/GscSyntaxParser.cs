using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
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
        /// Parses the GSC.xshd file from a stream and extracts all syntax highlighting data
        /// </summary>
        public static GscSyntaxData ParseGscXshd(Stream xshdStream)
        {
            if (xshdStream == null)
                throw new ArgumentNullException(nameof(xshdStream));

            var data = new GscSyntaxData();
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(xshdStream);

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

            // Parse keywords by category - iterate through all RuleSets to find Keywords
            // The .xshd file has Keywords in the main RuleSet and potentially nested RuleSets
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
                            // Use case-insensitive comparison for color names
                            string colorLower = colorAttr?.ToLowerInvariant() ?? "";
                            
                            switch (colorLower)
                            {
                                case "keywords":
                                    if (!data.Keywords.Contains(word, StringComparer.OrdinalIgnoreCase))
                                        data.Keywords.Add(word);
                                    break;
                                case "commandkeywords":
                                    if (!data.CommandKeywords.Contains(word, StringComparer.OrdinalIgnoreCase))
                                        data.CommandKeywords.Add(word);
                                    break;
                                case "preprocessorkeywords":
                                    if (!data.PreprocessorKeywords.Contains(word, StringComparer.OrdinalIgnoreCase))
                                        data.PreprocessorKeywords.Add(word);
                                    break;
                                case "builtinfunctions":
                                    if (!data.BuiltInFunctions.Contains(word, StringComparer.OrdinalIgnoreCase))
                                        data.BuiltInFunctions.Add(word);
                                    break;
                                case "truefalse":
                                    if (!data.TrueFalse.Contains(word, StringComparer.OrdinalIgnoreCase))
                                        data.TrueFalse.Add(word);
                                    break;
                                default:
                                    // If color attribute doesn't match known categories, 
                                    // try to infer from context or add to Keywords as fallback
                                    if (!string.IsNullOrEmpty(colorAttr))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Unknown keyword color category: {colorAttr} for word: {word}");
                                    }
                                    // Add to Keywords as fallback if no color specified
                                    if (string.IsNullOrEmpty(colorAttr) && !data.Keywords.Contains(word, StringComparer.OrdinalIgnoreCase))
                                    {
                                        data.Keywords.Add(word);
                                    }
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
        /// Gets a stream for the GSC.xshd file from embedded resources or file system
        /// Returns null if not found
        /// </summary>
        public static Stream GetGscXshdStream()
        {
            // First, try to load from embedded resources
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = "T7CompilerGUI.Resources.GSC.xshd";
            Stream stream = assembly.GetManifestResourceStream(resourceName);
            
            if (stream != null)
                return stream;
            
            // Fallback: try file system (for development)
            string xshdPath = GetDefaultGscXshdPath();
            if (!string.IsNullOrEmpty(xshdPath) && File.Exists(xshdPath))
            {
                return new FileStream(xshdPath, FileMode.Open, FileAccess.Read);
            }
            
            return null;
        }

        /// <summary>
        /// Gets the default GSC.xshd file path (for fallback when embedded resource not available)
        /// Checks Resources folder at solution root, then fallback locations
        /// </summary>
        public static string GetDefaultGscXshdPath()
        {
            // Try to find GSC.xshd in various locations
            // First check Resources folder at solution root (where the .sln file is)
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            
            // Try to find solution root by looking for TreyarchCompiler.sln
            string currentDir = baseDir;
            for (int i = 0; i < 5; i++)
            {
                string slnPath = Path.Combine(currentDir, "TreyarchCompiler.sln");
                if (File.Exists(slnPath))
                {
                    // Found solution root - check Resources folder
                    string resourcesPath = Path.Combine(currentDir, "Resources", "GSC.xshd");
                    if (File.Exists(resourcesPath))
                        return resourcesPath;
                    break;
                }
                currentDir = Path.GetDirectoryName(currentDir);
                if (string.IsNullOrEmpty(currentDir))
                    break;
            }
            
            // Fallback to other possible locations
            string[] possiblePaths = new[]
            {
                Path.Combine(baseDir, "GSC.xshd"),
                Path.Combine(baseDir, "Resources", "GSC.xshd"),
                Path.Combine(baseDir, "..", "Resources", "GSC.xshd"),
                Path.Combine(baseDir, "..", "..", "Resources", "GSC.xshd"),
                Path.Combine(baseDir, "..", "..", "..", "Resources", "GSC.xshd")
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

