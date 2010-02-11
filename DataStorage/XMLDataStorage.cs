using System;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Win32;
using System.Globalization;
using System.Collections.Generic;

using RightEdge.Common;

namespace RightEdge.DataStorage
{
	/// <summary>
	/// Summary description for XMLDataStorage.
	/// </summary>
	//public class XMLDataStorage : IBarDataStorage
	//{
	//    private string dataDirectory = "";
	//    private string lastError = "";

	//    public XMLDataStorage()
	//    {
	//        //  This used to try to create the data directory if it did not
	//        //  exist, but the code was badly broken and I am not sure it 
	//        //  was ever a good idea to do any of that here since this is 
	//        //  called even if XML is not the selected datastore.

	//        RegistryKey regKey = Registry.CurrentUser.OpenSubKey(@"Software\Yye Software\XMLDataStore");

	//        if (regKey != null)
	//        {
	//            object val = regKey.GetValue("Directory");
	//            if (val != null)
	//            {
	//                dataDirectory = val.ToString();
	//            }
	//            regKey.Close();
	//        }
	//    }

	//    public bool EnsureDataDir()
	//    {
	//        try
	//        {
	//            if (!Directory.Exists(dataDirectory))
	//            {
	//                Directory.CreateDirectory(dataDirectory);
	//            }
	//            return true;
	//        }
	//        catch (Exception e)
	//        {
	//            System.Diagnostics.Trace.WriteLine(e.Message);
	//            System.Diagnostics.Trace.WriteLine(e.StackTrace);
	//            return false;
	//        }
	//    }

	//    #region IBarDataStorage Members

	//    /// <summary>
	//    /// Retrieves a list of symbols from the data store with related symbol information
	//    /// </summary>
	//    /// <returns>a populated SymbolInformationCollection</returns>
	//    public List<SymbolInformation> GetSymbolList()
	//    {
	//        List<SymbolInformation> symbolInformationCollection = new List<SymbolInformation>();

	//        if (dataDirectory.Length == 0 || !Directory.Exists(dataDirectory))
	//        {
	//            return symbolInformationCollection;
	//        }

	//        string[] files = Directory.GetFiles(dataDirectory, "*.xml");

	//        foreach(string file in files)
	//        {
	//            TextReader streamReader = new StreamReader(file);
	//            streamReader.ReadLine();
	//            string s = streamReader.ReadLine();

	//            int x = s.IndexOf("<ArrayOfBarData");

	//            if (s.IndexOf("<ArrayOfBarData") != -1)
	//            {
	//                SymbolInformation symbolInformation = new SymbolInformation();

	//                int startIndex = file.LastIndexOf("\\");
	//                int index = file.LastIndexOf("_");
	//                int period = file.LastIndexOf(".");

	//                symbolInformation.Symbol = file.Substring(startIndex + 1, index - startIndex - 1);
	//                int frequency = int.Parse(file.Substring(index + 1, period - index - 1));
	//                symbolInformation.Frequency = frequency;

	//                symbolInformationCollection.Add(symbolInformation);
	//            }

	//            streamReader.Close();
	//        }

	//        return symbolInformationCollection;
	//    }

	//    /// <summary>
	//    /// Sets detailed symbol information about a particular symbol
	//    /// </summary>
	//    /// <param name="symbol">the string containing the symbol name</param>
	//    /// <param name="symbolInformation">populated SymbolInformation class containing the information to set about this symbol</param>
	//    public void SetSymbolInformation(string symbol, SymbolInformation symbolInformation)
	//    {
	//        // probably a more efficient way to do this, but
	//        // I don't think this will be getting called very much

	//        if (!EnsureDataDir())
	//        {
	//            return;
	//        }

	//        XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<SymbolInformation>));
	//        string symbolInfoFile = dataDirectory + "SymbolInformation.xml";
	//        List<SymbolInformation> symbolList = null;

	//        if (File.Exists(symbolInfoFile))
	//        {
	//            TextReader reader = new StreamReader(symbolInfoFile);
	//            symbolList = (List<SymbolInformation>)xmlSerializer.Deserialize(reader);
	//            if (reader != null)
	//            {
	//                reader.Close();
	//            }
	//        }
	//        else
	//        {
	//            symbolList = new List<SymbolInformation>();
	//        }

	//        if (symbolList != null)
	//        {
	//            SymbolInformation foundSymbolInfo = null;

	//            foreach (SymbolInformation symbolInfo in symbolList)
	//            {
	//                if (symbolInfo.Symbol == symbol)
	//                {
	//                    foundSymbolInfo = symbolInfo;
	//                    break;
	//                }
	//            }

	//            if (foundSymbolInfo != null)
	//            {
	//                symbolList.Remove(foundSymbolInfo);
	//            }

	//            symbolList.Add(symbolInformation);
	//        }
	//        else
	//        {
	//            symbolList = new List<SymbolInformation>();
	//            SymbolInformation symbolInfo = symbolInformation;
	//            symbolList.Add(symbolInfo);
	//        }

	//        StreamWriter streamWriter = new StreamWriter(symbolInfoFile);
	//        xmlSerializer = new XmlSerializer(typeof(List<SymbolInformation>));

	//        xmlSerializer.Serialize(streamWriter, symbolList);
	//        if (streamWriter != null)
	//        {
	//            streamWriter.Close();
	//        }
	//    }

	//    private void SaveSymbolInformation(List<SymbolInformation> symbolInfoCollection)
	//    {
	//        if (!EnsureDataDir())
	//        {
	//            return;
	//        }

	//        TextWriter streamWriter = null;

	//        try
	//        {
	//            string symbolInfoFile = dataDirectory + "SymbolInformation.xml";

	//            streamWriter = new StreamWriter(symbolInfoFile, false);
	//            XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<SymbolInformation>));

	//            xmlSerializer.Serialize(streamWriter, symbolInfoCollection);
	//        }
	//        finally
	//        {
	//            if (streamWriter != null)
	//            {
	//                streamWriter.Close();
	//            }
	//        }
	//    }

	//    public SymbolInformation GetSymbolInformation(string symbol)
	//    {
	//        SymbolInformation symbolInformation = null;

	//        XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<SymbolInformation>));
	//        string symbolInfoFile = dataDirectory + "SymbolInformation.xml";

	//        if (File.Exists(symbolInfoFile))
	//        {
	//            TextReader reader = new StreamReader(symbolInfoFile);
	//            List<SymbolInformation> symbolList = (List<SymbolInformation>)xmlSerializer.Deserialize(reader);

	//            if (symbolList != null)
	//            {
	//                foreach (SymbolInformation symbolInfo in symbolList)
	//                {
	//                    if (symbolInfo.Symbol == symbol)
	//                    {
	//                        symbolInformation = symbolInfo;
	//                        break;
	//                    }
	//                }
	//            }

	//            if (reader != null)
	//            {
	//                reader.Close();
	//            }
	//        }

	//        return symbolInformation;
	//    }

	//    /// <summary>
	//    /// Number of bars loaded from last call to LoadBars()
	//    /// </summary>
	//    /// <returns></returns>
	//    public int GetBarCount(string symbol, int frequency)
	//    {
	//        int barCount = 0;

	//        string fileName = dataDirectory + symbol + "_" + frequency.ToString() + ".xml";

	//        if (!File.Exists(fileName))
	//        {
	//            return -1;
	//        }

	//        StreamReader sr = new StreamReader(fileName);

	//        if (sr.BaseStream.Length > 1000)
	//        {
	//            // Let's only read the last 500 or so bytes.
	//            sr.BaseStream.Seek(sr.BaseStream.Length - 500, SeekOrigin.Begin);
	//        }
	//        string data = sr.ReadToEnd();

	//        int barCommentIndex = data.IndexOf("<!--");

	//        // First comment item is the count
	//        if (barCommentIndex != -1)
	//        {
	//            int barCommentEndIndex = data.IndexOf("-->", barCommentIndex);
	//            barCommentIndex += 4;		// Get past the first bit of comment string
	//            string bars = data.Substring(barCommentIndex, barCommentEndIndex - barCommentIndex);
	//            barCount = int.Parse(bars);
	//        }

	//        sr.Close();

	//        if (barCount == 0)
	//        {
	//            // Well, couldn't find it in the comment portion of the XML file
	//            // so let's load it the old fashioned (slow) way
	//            List<BarData> bars = LoadBars(symbol, frequency);
	//            barCount = bars.Count;
	//        }

	//        return barCount;
	//    }

	//    /// <summary>
	//    /// This will be the path where the xml data files are stored
	//    /// </summary>
	//    /// <param name="connectionData">fully qualified path to the xml data files</param>
	//    public void SetConnectionData(string connectionData)
	//    {
	//        dataDirectory = connectionData;
	//    }

	//    private string CleanFileName(string fileName)
	//    {
	//        fileName = fileName.Replace("/", "");
	//        fileName = fileName.Replace("*", "");
	//        fileName = fileName.Replace("?", "");

	//        return fileName;
	//    }

	//    /// <summary>
	//    /// Gets the last (most recent) bar date and time from the most recently loaded BarDataCollection
	//    /// </summary>
	//    /// <returns>DateTime of the most recent bar.  DateTime.MinValue if there are no bars loaded.</returns>
	//    public DateTime GetLastBarDate(string symbol, int frequency)
	//    {
	//        DateTime lastBarDate = DateTime.MinValue;

	//        string fileName = dataDirectory + symbol + "_" + frequency.ToString() + ".xml";
	//        fileName = CleanFileName(fileName);

	//        if (!File.Exists(fileName))
	//        {
	//            return DateTime.MinValue;
	//        }

	//        StreamReader sr = new StreamReader(fileName);


	//        if (sr.BaseStream.Length > 1000)
	//        {
	//            // Let's only read the last 500 or so bytes.
	//            sr.BaseStream.Seek(sr.BaseStream.Length - 500, SeekOrigin.Begin);
	//        }
	//        string data = sr.ReadToEnd();

	//        int barCommentIndex = data.IndexOf("<!--");

	//        if (barCommentIndex != -1)
	//        {
	//            // First comment item is the count
	//            barCommentIndex = data.IndexOf("<!--", barCommentIndex + 1);

	//            if (barCommentIndex != -1)
	//            {
	//                barCommentIndex = data.IndexOf("<!--", barCommentIndex + 1);

	//                if (barCommentIndex != -1)
	//                {
	//                    int barCommentEndIndex = data.IndexOf("-->", barCommentIndex);
	//                    barCommentIndex += 4;		// Get past the first bit of comment string
	//                    string lastDate = data.Substring(barCommentIndex, barCommentEndIndex - barCommentIndex);
	//                    //lastBarDate = DateTime.Parse(lastDate);
	//                    lastBarDate = DateTime.FromBinary(Convert.ToInt64(lastDate));
	//                }
	//            }
	//        }

	//        sr.Close();

	//        if (lastBarDate == DateTime.MinValue)
	//        {
	//            // Well, couldn't find it in the comment portion of the XML file
	//            // so let's load it the old fashioned (slow) way
	//            List<BarData> bars = LoadBars(symbol, frequency);

	//            if (bars.Count > 0)
	//            {
	//                lastBarDate = bars[bars.Count - 1].PriceDateTime;
	//            }

	//        }

	//        return lastBarDate;
	//    }

	//    /// <summary>
	//    /// Gets the first (most distant) bar date and time from the most recently loaded BarDataColleciton
	//    /// </summary>
	//    /// <returns>DateTime of the most distant bar, DateTime.MinValue if there are no bars loaded.</returns>
	//    public DateTime GetFirstBarDate(string symbol, int frequency)
	//    {
	//        DateTime firstBarDate = DateTime.MinValue;

	//        string fileName = dataDirectory + symbol + "_" + frequency.ToString() + ".xml";
	//        fileName = CleanFileName(fileName);
	//        if (!File.Exists(fileName))
	//        {
	//            return DateTime.MinValue;
	//        }

			
	//        StreamReader sr = new StreamReader(fileName);

	//        // Let's only read the last 500 or so bytes.
	//        if (sr.BaseStream.Length > 1000)
	//        {
	//            sr.BaseStream.Seek(sr.BaseStream.Length - 500, SeekOrigin.Begin);
	//        }
	//        string data = sr.ReadToEnd();

	//        int barCommentIndex = data.IndexOf("<!--");

	//        if (barCommentIndex != -1)
	//        {
	//            // First comment item is the count
	//            barCommentIndex = data.IndexOf("<!--", barCommentIndex + 1);

	//            if (barCommentIndex != -1)
	//            {
	//                int barCommentEndIndex = data.IndexOf("-->", barCommentIndex);
	//                barCommentIndex += 4;		// Get past the first bit of comment string
	//                string firstDate = data.Substring(barCommentIndex, barCommentEndIndex - barCommentIndex);
	//                firstBarDate = DateTime.FromBinary(Convert.ToInt64(firstDate));
	//                //firstBarDate = DateTime.Parse(firstDate);
	//            }
	//        }

	//        sr.Close();

	//        if (firstBarDate == DateTime.MinValue)
	//        {
	//            // Well, couldn't find it in the comment portion of the XML file
	//            // so let's load it the old fashioned (slow) way
	//            List<BarData> bars = LoadBars(symbol, frequency);

	//            if (bars.Count > 0)
	//            {
	//                firstBarDate = bars[0].PriceDateTime;
	//            }
	//        }

	//        return firstBarDate;
	//    }

	//    /// <summary>
	//    /// true if the data source requires authentication, such as a database.  This will enable the
	//    /// User Name and Password fields in the user interface
	//    /// </summary>
	//    /// <returns></returns>
	//    public bool RequiresAuthentication()
	//    {
	//        return false;
	//    }

	//    public List<BarData> LoadBars(string symbol, DateTime startDate, int frequency)
	//    {
	//        List<BarData> bars = new List<BarData>();
	//        XmlSerializer x = new XmlSerializer(typeof(List<BarData>));
	//        string fileName = dataDirectory + symbol + "_" + frequency.ToString() + ".xml";

	//        fileName = CleanFileName(fileName);

	//        if (File.Exists(fileName))
	//        {
	//            TextReader textReader = new StreamReader(fileName, false);
	//            bars = (List<BarData>)x.Deserialize(textReader);
	//            //bars = BuildList(fileName);
	//            textReader.Close();
	//        }

	//        return bars;
	//    }

	//    public List<BarData> LoadLastBars(string symbol, int frequency, int barCount)
	//    {
	//        // LoadLastBars doesn't actually provide any sort of performance boost
	//        // at all.  At this point, the XML and SQL Server Everywhere data store
	//        // are the only two stores that don't have a way to get a portion of
	//        // their bar data from disk/database (and I'm not sure that we care
	//        // at this point).
	//        List<BarData> bars = LoadBars(symbol, frequency);

	//        if (barCount > bars.Count)
	//        {
	//            barCount = bars.Count;
	//        }

	//        List<BarData> lastBars = bars.GetRange((bars.Count - 1) - barCount, barCount);

	//        return lastBars;
	//    }

	//    public List<BarData> BuildList(string fileName)
	//    {
	//        List<BarData> bars = new List<BarData>();

	//        if (!File.Exists(fileName))
	//        {
	//            return bars;
	//        }

	//        XmlDataDocument xmlDocument = new XmlDataDocument();

	//        XmlTextReader xmlReader = new XmlTextReader(fileName);
	//        xmlReader.Read();
	//        //xmlReader.MoveToFirstAttribute();
	//        xmlReader.MoveToContent();
	//        xmlDocument.Load(xmlReader);

	//        foreach(XmlNode node in xmlDocument.ChildNodes[0])
	//        {
	//            bars.Add(LoadBar(node));
	//        }

	//        xmlReader.Close();

	//        return bars;
	//    }

	//    private BarData LoadBar(XmlNode StartNode)
	//    {
	//        BarData barNode = new BarData();
	//        IFormatProvider culture = new CultureInfo("en-US");

	//        foreach(XmlNode childNode in StartNode.ChildNodes)
	//        {
	//            if (childNode.Name == "PriceDateTime")
	//            {
	//                barNode.PriceDateTime = Convert.ToDateTime(childNode.InnerText);
	//            }

	//            if (childNode.Name == "Open")
	//            {
	//                barNode.Open = double.Parse(childNode.InnerText, NumberStyles.Number, culture);
	//                //barNode.Open = Convert.ToDouble(childNode.InnerText);
	//            }

	//            if (childNode.Name == "Close")
	//            {
	//                barNode.Close = double.Parse(childNode.InnerText, NumberStyles.Number, culture);
	//            }

	//            if (childNode.Name == "High")
	//            {
	//                barNode.High = double.Parse(childNode.InnerText, NumberStyles.Number, culture);
	//            }

	//            if (childNode.Name == "Low")
	//            {
	//                barNode.Low = double.Parse(childNode.InnerText, NumberStyles.Number, culture);
	//            }

	//            if (childNode.Name == "Bid")
	//            {
	//                barNode.Bid = double.Parse(childNode.InnerText, NumberStyles.Number, culture);
	//            }

	//            if (childNode.Name == "Ask")
	//            {
	//                barNode.Ask = double.Parse(childNode.InnerText, NumberStyles.Number, culture);
	//            }

	//            if (childNode.Name == "Volume")
	//            {
	//                barNode.Volume = Convert.ToUInt64(childNode.InnerText);
	//            }

	//            if (childNode.Name == "OpenInterest")
	//            {
	//                barNode.OpenInterest = Convert.ToInt32(childNode.InnerText);
	//            }

	//            if (childNode.Name == "EmptyBar")
	//            {
	//                barNode.EmptyBar = bool.Parse(childNode.InnerText);
	//            }
	//        }

	//        return barNode;
	//    }

	//    public bool ConnectionOpen()
	//    {
	//        if (dataDirectory.Length == 0)
	//        {
	//            lastError = "A data directory has not been configured.";
	//            return false;
	//        }

	//        return true;
	//    }

	//    public void DoSettings()
	//    {
	//        XMLDataStoreSettings dlg = new XMLDataStoreSettings();
			
	//        dlg.DataDirectory = dataDirectory;
			
	//        if (dlg.ShowDialog() == DialogResult.OK)
	//        {
	//            try
	//            {
	//                RegistryKey regKey = Registry.CurrentUser.CreateSubKey(@"Software\Yye Software\XMLDataStore");
					
	//                dataDirectory = dlg.DataDirectory;

	//                //  Do this so we never save (or have in memory)
	//                //  an invalid directory name.  We should also
	//                //  check to make sure this exists at the time
	//                //  of save and complain if it does not or offer
	//                //  the option to create the directory.
	//                if (!dataDirectory.EndsWith("\\"))
	//                {
	//                    dataDirectory += "\\";
	//                }

	//                regKey.SetValue("Directory", dataDirectory);
	//                regKey.Close();
	//            }
	//            catch(Exception exception)
	//            {
	//                lastError = exception.Message;
	//            }
	//        }
	//    }

	//    public bool IsProperlyConfigured()
	//    {
	//        if (dataDirectory.Length > 0)
	//        {
	//            return true;
	//        }
	//        else
	//        {
	//            return false;
	//        }
	//    }

	//    public string LastError()
	//    {
	//        return lastError;
	//    }

	//    public bool PerformConnection()
	//    {
	//        if (dataDirectory.Length == 0)
	//        {
	//            lastError = "A data directory has not been configured.";
	//            return false;
	//        }

	//        return true;
	//    }

	//    public void ForceDefaultSettings()
	//    {
	//        RegistryKey regKey = Registry.CurrentUser.CreateSubKey(@"Software\Yye Software\XMLDataStore");
	//        string dataDir = System.IO.Directory.GetCurrentDirectory();

	//        if (!dataDir.EndsWith("\\"))
	//        {
	//            dataDir += "\\";
	//        }

	//        dataDir += "Stock Data\\";

	//        this.EnsureDataDir();
	//        regKey.SetValue("Directory", dataDir);
	//        regKey.Close();
	//    }

	//    public bool RequiresSetup()
	//    {
	//        return true;
	//    }

	//    public List<BarData> LoadBars(string symbol, int frequency)
	//    {
	//        return LoadBars(symbol, DateTime.MinValue, frequency);
	//    }

	//    /// <summary>
	//    /// Saves the bars (overwriting any existing bars) for the specified symbol for the specified period
	//    /// </summary>
	//    /// <param name="symbol">symbol name to persist</param>
	//    /// <param name="bars">fully populated bar collection to persist</param>
	//    /// <param name="frequency"></param>
	//    public int SaveBars(string symbol, List<BarData> bars, int frequency)
	//    {
	//        bars.Sort(new DateComparer(DateComparer.SortOrder.Ascending));

	//        if (!EnsureDataDir())
	//        {
	//            return -1;
	//        }

	//        string fileName = dataDirectory + symbol + "_" + frequency.ToString() + ".xml";
	//        fileName = CleanFileName(fileName);
			
	//        TextWriter textWriter = new StreamWriter(fileName, false);

	//        XmlSerializer x = new XmlSerializer(typeof(List<BarData>));
	//        x.Serialize(textWriter, bars);

	//        if (bars.Count > 0)
	//        {
	//            textWriter.WriteLine("");
	//            textWriter.WriteLine("<!--" + bars.Count.ToString() + "-->");
	//            textWriter.WriteLine("<!--" + bars[0].PriceDateTime.ToBinary() + "-->");
	//            textWriter.WriteLine("<!--" + bars[bars.Count - 1].PriceDateTime.ToBinary() + "-->");
	//        }

	//        textWriter.Flush();
	//        textWriter.Close();

	//        return bars.Count;
	//    }

	//    // This is the function to use if we're going to not use XmlSerializer for the bar data
	//    // collection.  However, be warned that this is VERY slow (about 100 times slower than the
	//    // serializer shit.
	//    private void PersistBarData(string fileName, List<BarData> bars)
	//    {
	//        XmlDataDocument xmlDocument = new XmlDataDocument();

	//        XmlNode root = null;
	//        root = xmlDocument.DocumentElement;

	//        if (root != null)
	//        {
	//            root.RemoveAll();	// Build from scratch
	//        }
	//        else	// New file
	//        {
	//            XmlElement Root = xmlDocument.CreateElement("ArrayOfBarData");
	//            xmlDocument.AppendChild(Root);
	//            root = (XmlNode)Root;
	//        }

	//        XmlElement TopElem = (XmlElement)root;
	//        TopElem.SetAttribute("count", bars.Count.ToString());
	//        TopElem.SetAttribute("LastRetrievalDate", bars[bars.Count - 1].PriceDateTime.ToShortDateString());
	//        foreach(BarData bar in bars)
	//        {
	//            PersistNode(xmlDocument, bar, TopElem);
	//            xmlDocument.AppendChild(TopElem);
	//        }

	//        XmlTextWriter xmlWriter = new XmlTextWriter(fileName, System.Text.Encoding.ASCII);
	//        xmlWriter.Formatting = Formatting.Indented;
	//        xmlWriter.Indentation = 1;
	//        xmlWriter.IndentChar = (char)9;
	//        xmlDocument.WriteTo(xmlWriter);
	//        xmlWriter.Flush();
	//        xmlWriter.Close();
	//    }

	//    private XmlElement PersistNode(XmlDataDocument xmlDocument, BarData node, XmlElement TopLevel)
	//    {
	//        XmlElement TopElem = xmlDocument.CreateElement("BarData");
	//        TopLevel.AppendChild(TopElem);

	//        XmlElement elem = null;

	//        // PriceDateTime
	//        elem = xmlDocument.CreateElement("PriceDateTime");
	//        XmlText text = xmlDocument.CreateTextNode(node.PriceDateTime.ToString());
	//        TopElem.AppendChild(elem);
	//        TopElem.LastChild.AppendChild(text);

	//        // Open
	//        elem = xmlDocument.CreateElement("Open");
	//        text = xmlDocument.CreateTextNode(node.Open.ToString());
	//        TopElem.AppendChild(elem);
	//        TopElem.LastChild.AppendChild(text);

	//        // Close
	//        elem = xmlDocument.CreateElement("Close");
	//        text = xmlDocument.CreateTextNode(node.Close.ToString());
	//        TopElem.AppendChild(elem);
	//        TopElem.LastChild.AppendChild(text);

	//        // High
	//        elem = xmlDocument.CreateElement("High");
	//        text = xmlDocument.CreateTextNode(node.High.ToString());
	//        TopElem.AppendChild(elem);
	//        TopElem.LastChild.AppendChild(text);

	//        // Low
	//        elem = xmlDocument.CreateElement("Low");
	//        text = xmlDocument.CreateTextNode(node.Low.ToString());
	//        TopElem.AppendChild(elem);
	//        TopElem.LastChild.AppendChild(text);

	//        // Bid
	//        elem = xmlDocument.CreateElement("Bid");
	//        text = xmlDocument.CreateTextNode(node.Bid.ToString());
	//        TopElem.AppendChild(elem);
	//        TopElem.LastChild.AppendChild(text);

	//        // Ask
	//        elem = xmlDocument.CreateElement("Ask");
	//        text = xmlDocument.CreateTextNode(node.Ask.ToString());
	//        TopElem.AppendChild(elem);
	//        TopElem.LastChild.AppendChild(text);

	//        // Volume
	//        elem = xmlDocument.CreateElement("Volume");
	//        text = xmlDocument.CreateTextNode(node.Volume.ToString());
	//        TopElem.AppendChild(elem);
	//        TopElem.LastChild.AppendChild(text);

	//        // OpenInterest
	//        elem = xmlDocument.CreateElement("OpenInterest");
	//        text = xmlDocument.CreateTextNode(node.OpenInterest.ToString());
	//        TopElem.AppendChild(elem);
	//        TopElem.LastChild.AppendChild(text);

	//        // OpenInterest
	//        elem = xmlDocument.CreateElement("EmptyBar");
	//        text = xmlDocument.CreateTextNode(node.EmptyBar.ToString());
	//        TopElem.AppendChild(elem);
	//        TopElem.LastChild.AppendChild(text);

	//        TopElem.AppendChild(elem);

	//        return TopElem;
	//    }
	//    /// <summary>
	//    /// Updates bars for an existing symbol leaving the currently stored bar collection in tact
	//    /// </summary>
	//    /// <param name="symbol">symbol name to update</param>
	//    /// <param name="bars">BarDataCollection to apply to the data store</param>
	//    /// <param name="frequency">BarFrequency of this BarDataCollection</param>
	//    public int UpdateBars(string symbol, List<BarData> bars, int frequency)
	//    {
	//        List<BarData> currentBars = LoadBars(symbol, DateTime.MinValue, frequency);
	//        int updatedBars = 0;

	//        if (currentBars == null)
	//        {
	//            currentBars = bars;
	//            updatedBars = currentBars.Count;
	//        }
	//        else
	//        {
	//            foreach(BarData bar in bars)
	//            {
	//                if (!BarExists(currentBars, bar))
	//                {
	//                    currentBars.Add(bar);
	//                    updatedBars++;
	//                }
	//            }
	//        }

	//        SaveBars(symbol, currentBars, frequency);

	//        return updatedBars;
	//    }

	//    /// <summary>
	//    /// Determines if a "new" bar (one that is most likely looking to be added) already exists in a specified collection
	//    /// </summary>
	//    /// <param name="sourceCollection">the source BarDataCollection to examine</param>
	//    /// <param name="newBar">the new bar to search for in the source collection</param>
	//    /// <returns>true if the bar is found in the existing collection, else false</returns>
	//    private bool BarExists(List<BarData> sourceCollection, BarData newBar)
	//    {
	//        foreach(BarData bar in sourceCollection)
	//        {
	//            if (bar.PriceDateTime == newBar.PriceDateTime)
	//            {
	//                return true;
	//            }
	//        }

	//        return false;
	//    }

	//    public void DeleteBars(string symbol, int frequency)
	//    {
	//        string fileName = dataDirectory + symbol + "_" + frequency.ToString() + ".xml";
	//        fileName = CleanFileName(fileName);

	//        if (File.Exists(fileName))
	//        {
	//            File.Delete(fileName);
	//        }
	//    }

	//    public void SaveTick(string symbol, TickData tick)
	//    {
	//        List<TickData> currentTicks = LoadTicks(symbol, DateTime.MinValue);
	//        currentTicks.Add(tick);
	//        currentTicks.Sort(new TickDateComparer());
	//        SaveTicks(symbol, currentTicks);
	//    }


	//    public int SaveTicks(string symbol, List<TickData> ticks)
	//    {
	//        if (!EnsureDataDir())
	//        {
	//            return -1;
	//        }

	//        ticks.Sort(new TickDateComparer());
	//        string filename = dataDirectory + symbol + "_tick.xml";
	//        filename = CleanFileName(filename);

	//        XmlSerializer x = new XmlSerializer(typeof(List<TickData>));
	//        TextWriter textWriter = new StreamWriter(filename, false);
	//        x.Serialize(textWriter, ticks);

	//        textWriter.WriteLine("");
	//        textWriter.WriteLine("<!--" + ticks.Count.ToString() + "-->");
	//        textWriter.WriteLine("<!--" + ticks[0].time.ToString() + "-->");
	//        textWriter.WriteLine("<!--" + ticks[ticks.Count - 1].time.ToString() + "-->");

	//        textWriter.Close();

	//        return ticks.Count;
	//    }

	//    public int UpdateTicks(string symbol, List<TickData> newTicks)
	//    {
	//        int nUpdated = newTicks.Count;
	//        if (newTicks.Count == 0)
	//        {
	//            return 0;
	//        }

	//        List<TickData> currentTicks = LoadTicks(symbol, DateTime.MinValue);

	//        newTicks.Sort(new TickDateComparer());
	//        DateTime firstDate = newTicks[0].time;
	//        DateTime lastDate = newTicks[newTicks.Count - 1].time;

	//        foreach (TickData tick in currentTicks)
	//        {
	//            if (tick.time < firstDate || tick.time > lastDate)
	//            {
	//                newTicks.Add(tick);
	//            }
	//            else
	//            {
	//                nUpdated--;
	//            }
	//        }
			
	//        SaveTicks(symbol, newTicks);

	//        return nUpdated;
	//    }

	//    public List<TickData> LoadTicks(string symbol, DateTime startDate)
	//    {
	//        List<TickData> ticks = new List<TickData>();
	//        XmlSerializer x = new XmlSerializer(typeof(List<TickData>));
	//        string fileName = dataDirectory + symbol + "_tick.xml";
	//        fileName = CleanFileName(fileName);
	//        if (File.Exists(fileName))
	//        {
	//            TextReader textReader = new StreamReader(fileName, false);
	//            try
	//            {
	//                ticks = (List<TickData>)x.Deserialize(textReader);
	//            }
	//            finally
	//            {
	//                textReader.Close();
	//            }
	//        }
	//        return ticks;
	//    }

	//    public bool FrequencySupported(string symbol, int frequency)
	//    {
	//        int lowestFrequency = int.MaxValue;

	//        foreach (int knownFrequency in Enum.GetValues(typeof(BarFrequency)))
	//        {
	//            string fileName = dataDirectory + symbol + "_" + knownFrequency.ToString() + ".xml";
	//            if (File.Exists(fileName))
	//            {
	//                lowestFrequency = Math.Min(lowestFrequency, knownFrequency);
	//            }
	//        }

	//        if (lowestFrequency <= frequency)
	//        {
	//            return true;
	//        }
	//        else
	//        {
	//            return false;
	//        }
	//    }

	//    public string CompanyName()
	//    {
	//        return Globals.Author;
	//    }

	//    public string GetName()
	//    {
	//        return "Local XML File";
	//    }

	//    public void SetUserName(string userName)
	//    {
	//    }

	//    public void SetPassword(string password)
	//    {
	//    }

	//    public string Version()
	//    {
	//        return Globals.Version;
	//    }

	//    public string id()
	//    {
	//        return "{83CD47DF-14DC-45fe-AF6C-0E0995DC2A95}";
	//    }

	//    public string GetDescription()
	//    {
	//        return "Stores market data locally in an XML format.  Not recommended for extremely large symbol lists or large amounts of data.";
	//    }

	//    #endregion

	//    //	Copied from PluginHelper
	//    static public string GetFullPath(string path)
	//    {
	//        if (path.StartsWith("\\") || path.IndexOf(':') >= 0)
	//        {
	//            //	absolute path
	//        }
	//        else
	//        {
	//            string exepath = System.Windows.Forms.Application.ExecutablePath;
	//            int lastBackslash = exepath.LastIndexOf('\\');
	//            if (lastBackslash >= 0)
	//            {
	//                exepath = exepath.Substring(0, lastBackslash + 1);
	//            }
	//            else
	//            {
	//                exepath = "";
	//            }
	//            path = exepath + path;
	//        }

	//        if (!path.EndsWith("\\"))
	//        {
	//            path = path + "\\";
	//        }

	//        return path;

	//    }
	//}
}
