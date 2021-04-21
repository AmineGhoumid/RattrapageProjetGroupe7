using System;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace easysave
{
    // This class is only used to write XML file, used by state file as well as the daily log file
    /*
     *  The file will looks like this
     * 
     * <?xml version="1.0" encoding="UTF-8"?>
     * <xmlsessionname>
	 *       <columnname>DATANAME FOR COLUMN NAME</columnname>
     * </xmlsessionname>
     * 
     */
    static class log
    {
        public static Object verrou = new object();
        // Write an xml, with only public function of this static class
        private static void writeXML(string filename, int howmanyobject, string xmlsessionname, string[] columnname, string[] dataforcolumn)
        {
            // Check if the file already exists ?
            if (!File.Exists(filename)) { createXML(filename, howmanyobject, xmlsessionname, columnname, dataforcolumn); } else { appendXML(filename, howmanyobject, xmlsessionname, columnname, dataforcolumn); }
        }
        // Replace specific XMLsession by another
        public static void replaceXmlSession(string filename, string xmlsessionnameToReplace, int howmanyobjectToSubstitute, string[] columnName, string[] dataForColumn)
        {
            lock (verrou)
            {
                try
                {
                    // identify the XMLsession first and last beacon
                    int lineFirstBeacon = -1;
                    int lineSecondBeacon = -1;
                    bool beaconIdentified = false;
                    int i = 0;
                    // If the XML file doesn't exist, we create it and exit
                    if (!File.Exists(filename)) { writeXML(filename, howmanyobjectToSubstitute, xmlsessionnameToReplace, columnName, dataForColumn); return; }
                    foreach (string line in File.ReadLines(filename))
                    {
                        if (line.Contains("<" + xmlsessionnameToReplace + ">"))
                        {
                            lineFirstBeacon = i;
                            beaconIdentified = true;
                        }
                        if (line.Contains("</" + xmlsessionnameToReplace + ">"))
                        {
                            lineSecondBeacon = i + 1; // Because i is incremented at the end
                            beaconIdentified = true;
                        }
                        i++;
                    }
                    // If no beacon, create the XML file or append the session
                    if (!beaconIdentified) { writeXML(filename, howmanyobjectToSubstitute, xmlsessionnameToReplace, columnName, dataForColumn); return; }
                    // Our beacons are identified (normally)
                    // Then we delete everything between these two beacons
                    var file = new List<string>(System.IO.File.ReadAllLines(filename));
                    for (int i_ = lineFirstBeacon; i_ < lineSecondBeacon; i_++) // We delete everything about the last XMLsession
                    {
                        file[i_] = "";
                    }
                    // We write another time the file without the beacon 
                    File.WriteAllLines(filename, file.ToArray());
                    // We add the new XMLsession
                    appendXML(filename, howmanyobjectToSubstitute, xmlsessionnameToReplace, columnName, dataForColumn);
                }
                catch
                {
                    replaceXmlSession(filename, xmlsessionnameToReplace, howmanyobjectToSubstitute, columnName, dataForColumn);
                }
            }
        }

        public static void deleteXmlSession(string filename, string xmlsessionname)
        {
            lock (verrou)
            {
                try
                {
                    // identify the XMLsession first and last beacon
                    int lineFirstBeacon = -1;
                    int lineSecondBeacon = -1;
                    int i = 0;
                    // If the XML file doesn't exist, we create it and exit
                    foreach (string line in File.ReadLines(filename))
                    {
                        if (line.Contains("<" + xmlsessionname + ">"))
                        {
                            lineFirstBeacon = i;
                        }
                        if (line.Contains("</" + xmlsessionname + ">"))
                        {
                            lineSecondBeacon = i + 1; // Because i is incremented at the end
                        }
                        i++;
                    }
                    // Our beacons are identified (normally)
                    // Then we delete everything between these two beacons
                    var file = new List<string>(System.IO.File.ReadAllLines(filename));
                    for (int i_ = lineFirstBeacon; i_ < lineSecondBeacon; i_++) // We delete everything about the last XMLsession
                    {
                        file[i_] = "";
                    }
                    // We write another time the file without the beacon 
                    File.WriteAllLines(filename, file.ToArray());
                }
                catch
                {
                    deleteXmlSession(filename, xmlsessionname);
                }
            }
        }

        // This function is called by the writeXML when the file is identified as already existant.
        private static void appendXML(string filename, int howmanyobject, string xmlsessionname, string[] columnname, string[] dataforcolumn) {
            string newfilecontent = Environment.NewLine; // Start the XML session (a session = a function call)
            newfilecontent += "  <" + xmlsessionname + ">" + Environment.NewLine;
            for (int i = 0; i<howmanyobject;i++)  // Write loop in XML file
            {
                newfilecontent += "     <" + columnname[i] + ">";
                newfilecontent += dataforcolumn[i] + "</"+ columnname[i] + ">" + Environment.NewLine;
            }
            newfilecontent += "  </" + xmlsessionname + ">" + Environment.NewLine; // End XML session
            string lastLine = File.ReadLines(filename).Last(); // Get XML root
            var lines = System.IO.File.ReadAllLines(filename); // Delete XML root
            System.IO.File.WriteAllLines(filename, lines.Take(lines.Length - 1).ToArray());
            File.AppendAllText(filename, newfilecontent);
            File.AppendAllText(filename, lastLine); // Put back the root
        }

        // This function is called by the writeXML function when the file is not identified as already existant.
        private static void createXML(string filename, int howmanyobject, string xmlsessionname, string[] columnname, string[] dataforcolumn) {
            string newfilecontent = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + Environment.NewLine + "<root>" + Environment.NewLine; // Start XML session (a session = a function call)
            newfilecontent += "  <" + xmlsessionname + ">" + Environment.NewLine;
            for (int i = 0; i < howmanyobject; i++)  // Write loop in XML file
            {
                newfilecontent += "     <" + columnname[i] + ">";
                newfilecontent += dataforcolumn[i] + "</" + columnname[i] + ">" + Environment.NewLine;
            }
            newfilecontent += "  </" + xmlsessionname + ">" + Environment.NewLine; // End XML session
            newfilecontent += "</root>"; 
            File.AppendAllText(filename, newfilecontent);
        }
    }
}
