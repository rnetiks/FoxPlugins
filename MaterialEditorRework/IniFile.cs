using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MaterialEditorRework
{
	/// <summary>
	/// A simple INI file parser compatible with .NET 3.5
	/// Supports sections, key-value pairs, and comments
	/// </summary>
	public class IniFile
	{
		private Dictionary<string, Dictionary<string, string>> sections;
		private Dictionary<string, List<string>> sectionComments;
		private Dictionary<string, Dictionary<string, string>> keyComments;
		private List<string> headerComments;
		private string filePath;


		public IniFile()
		{
			sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
			sectionComments = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
			keyComments = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
			headerComments = new List<string>();
		}

		public IniFile(string path) : this()
		{
			filePath = path;
			if (File.Exists(path))
			{
				Load(path);
			}
		}

		/// <summary>
		/// Load an INI file from disk
		/// </summary>
		public void Load(string path)
		{
			filePath = path;
			sections.Clear();
			sectionComments.Clear();
			keyComments.Clear();
			headerComments.Clear();

			if (!File.Exists(path))
			{
				throw new FileNotFoundException("INI file not found", path);
			}

			string[] lines = File.ReadAllLines(path);
			string currentSection = "";
			List<string> pendingComments = new List<string>();

			foreach (string line in lines)
			{
				string trimmed = line.Trim();
				if (string.IsNullOrEmpty(trimmed))
				{
					continue;
				}
				if (trimmed.StartsWith(";") || trimmed.StartsWith("#"))
				{
					pendingComments.Add(trimmed);
					continue;
				}
				if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
				{
					currentSection = trimmed.Substring(1, trimmed.Length - 2).Trim();

					if (!sections.ContainsKey(currentSection))
					{
						sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
						keyComments[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
					}
					if (pendingComments.Count > 0)
					{
						if (string.IsNullOrEmpty(currentSection))
						{
							headerComments.AddRange(pendingComments);
						}
						else
						{
							sectionComments[currentSection] = new List<string>(pendingComments);
						}
						pendingComments.Clear();
					}
					continue;
				}
				int equalsIndex = trimmed.IndexOf('=');
				if (equalsIndex > 0)
				{
					string key = trimmed.Substring(0, equalsIndex).Trim();
					string value = trimmed.Substring(equalsIndex + 1).Trim();
					int commentIndex = -1;
					for (int i = 0; i < value.Length; i++)
					{
						if (value[i] == ';' || value[i] == '#')
						{
							int quotesBefore = 0;
							for (int j = 0; j < i; j++)
							{
								if (value[j] == '"') quotesBefore++;
							}
							if (quotesBefore % 2 == 0)
							{
								commentIndex = i;
								break;
							}
						}
					}

					if (commentIndex >= 0)
					{
						value = value.Substring(0, commentIndex).Trim();
					}
					if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
					{
						value = value.Substring(1, value.Length - 2);
					}

					if (string.IsNullOrEmpty(currentSection))
					{
						currentSection = "";
						if (!sections.ContainsKey(currentSection))
						{
							sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
							keyComments[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
						}
					}

					sections[currentSection][key] = value;
					if (pendingComments.Count > 0)
					{
						keyComments[currentSection][key] = string.Join(Environment.NewLine, pendingComments.ToArray());
						pendingComments.Clear();
					}
				}
			}
			if (pendingComments.Count > 0)
			{
				headerComments.AddRange(pendingComments);
			}
		}

		/// <summary>
		/// Save the INI file to disk
		/// </summary>
		public void Save(string path = null)
		{
			if (path == null)
			{
				path = filePath;
			}

			if (string.IsNullOrEmpty(path))
			{
				throw new ArgumentException("No file path specified");
			}

			StringBuilder sb = new StringBuilder();
			foreach (string comment in headerComments)
			{
				sb.AppendLine(comment);
			}
			bool firstSection = true;
			foreach (var section in sections)
			{
				if (!firstSection || headerComments.Count > 0)
				{
					sb.AppendLine();
				}
				firstSection = false;
				if (sectionComments.ContainsKey(section.Key))
				{
					foreach (string comment in sectionComments[section.Key])
					{
						sb.AppendLine(comment);
					}
				}
				if (!string.IsNullOrEmpty(section.Key))
				{
					sb.AppendLine("[" + section.Key + "]");
				}
				foreach (var kvp in section.Value)
				{
					if (keyComments.ContainsKey(section.Key) &&
						keyComments[section.Key].ContainsKey(kvp.Key))
					{
						sb.AppendLine(keyComments[section.Key][kvp.Key]);
					}
					string value = kvp.Value;
					if (value.Contains(";") || value.Contains("#") ||
						value.StartsWith(" ") || value.EndsWith(" "))
					{
						value = "\"" + value + "\"";
					}
					sb.AppendLine(kvp.Key + " = " + value);
				}
			}

			File.WriteAllText(path, sb.ToString());
			filePath = path;
		}

		/// <summary>
		/// Read a value from the INI file
		/// </summary>
		public string Read(string section, string key, string defaultValue = "")
		{
			if (section == null) section = "";

			if (sections.ContainsKey(section) && sections[section].ContainsKey(key))
			{
				return sections[section][key];
			}
			return defaultValue;
		}

		/// <summary>
		/// Write a value to the INI file
		/// </summary>
		public void Write(string section, string key, string value)
		{
			if (section == null) section = "";

			if (!sections.ContainsKey(section))
			{
				sections[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				keyComments[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			}

			sections[section][key] = value;
		}

		/// <summary>
		/// Delete a key from a section
		/// </summary>
		public void DeleteKey(string section, string key)
		{
			if (section == null) section = "";

			if (sections.ContainsKey(section))
			{
				sections[section].Remove(key);
				if (keyComments.ContainsKey(section))
				{
					keyComments[section].Remove(key);
				}
			}
		}

		/// <summary>
		/// Delete an entire section
		/// </summary>
		public void DeleteSection(string section)
		{
			if (section == null) section = "";

			sections.Remove(section);
			sectionComments.Remove(section);
			keyComments.Remove(section);
		}

		/// <summary>
		/// Check if a section exists
		/// </summary>
		public bool SectionExists(string section)
		{
			if (section == null) section = "";
			return sections.ContainsKey(section);
		}

		/// <summary>
		/// Check if a key exists in a section
		/// </summary>
		public bool KeyExists(string section, string key)
		{
			if (section == null) section = "";
			return sections.ContainsKey(section) && sections[section].ContainsKey(key);
		}

		/// <summary>
		/// Get all section names
		/// </summary>
		public string[] GetSections()
		{
			string[] result = new string[sections.Keys.Count];
			sections.Keys.CopyTo(result, 0);
			return result;
		}

		/// <summary>
		/// Get all keys in a section
		/// </summary>
		public string[] GetKeys(string section)
		{
			if (section == null) section = "";

			if (sections.ContainsKey(section))
			{
				string[] result = new string[sections[section].Keys.Count];
				sections[section].Keys.CopyTo(result, 0);
				return result;
			}
			return new string[0];
		}

		/// <summary>
		/// Add a comment above a section
		/// </summary>
		public void AddSectionComment(string section, string comment)
		{
			if (section == null) section = "";

			if (!comment.StartsWith(";") && !comment.StartsWith("#"))
			{
				comment = "; " + comment;
			}

			if (!sectionComments.ContainsKey(section))
			{
				sectionComments[section] = new List<string>();
			}

			sectionComments[section].Add(comment);
		}

		/// <summary>
		/// Add a comment above a key
		/// </summary>
		public void AddKeyComment(string section, string key, string comment)
		{
			if (section == null) section = "";

			if (!comment.StartsWith(";") && !comment.StartsWith("#"))
			{
				comment = "; " + comment;
			}

			if (!keyComments.ContainsKey(section))
			{
				keyComments[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			}

			keyComments[section][key] = comment;
		}

		/// <summary>
		/// Add a header comment at the top of the file
		/// </summary>
		public void AddHeaderComment(string comment)
		{
			if (!comment.StartsWith(";") && !comment.StartsWith("#"))
			{
				comment = "; " + comment;
			}
			headerComments.Add(comment);
		}
		public int ReadInt(string section, string key, int defaultValue = 0)
		{
			string value = Read(section, key, defaultValue.ToString());
			int result;
			if (int.TryParse(value, out result))
				return result;
			return defaultValue;
		}

		public void WriteInt(string section, string key, int value)
		{
			Write(section, key, value.ToString());
		}

		public bool ReadBool(string section, string key, bool defaultValue = false)
		{
			string value = Read(section, key, defaultValue.ToString()).ToLower();
			return value == "true" || value == "1" || value == "yes" || value == "on";
		}

		public void WriteBool(string section, string key, bool value)
		{
			Write(section, key, value.ToString().ToLower());
		}

		public float ReadFloat(string section, string key, float defaultValue = 0f)
		{
			string value = Read(section, key, defaultValue.ToString());
			float result;
			if (float.TryParse(value, out result))
				return result;
			return defaultValue;
		}

		public void WriteFloat(string section, string key, float value)
		{
			Write(section, key, value.ToString());
		}

		public double ReadDouble(string section, string key, double defaultValue = 0.0)
		{
			string value = Read(section, key, defaultValue.ToString());
			double result;
			if (double.TryParse(value, out result))
				return result;
			return defaultValue;
		}

		public void WriteDouble(string section, string key, double value)
		{
			Write(section, key, value.ToString());
		}
	}
}