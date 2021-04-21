using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace easysave
{
    // This class is used to handle differently each different save type, cannot be instancied. 
    // This is this class that will be passed to the holder. The daughter-class mus inherit from this class to be saved.
    // All the path must be UNC
    // Factory design pattern
    public abstract class saveWork 
    {
        public string appellation; // Save name of the save
        public string sourceDir; // Save source
        public string destDir; // Save destination 
        public int mode; // Save mode with number, if 0 this is full and 1 for differential
        public float statusPercentage; // Save status 0 - 100 %
        public double tempsEcoule; // Elipsed time
        public string horodatage; // Timestamp
        public double cryptTime; // Used to get the amount of time spent in crypting.
        public abstract void save(); // Must be redefined for each save type
        public Object progressDisplay;
        public Object pauseDisplay;
        public Object stopDisplay;

        public bool isPaused = false;
        public bool stop = false;

        public string[] fileBuffer; // Source file name that are not prioritary. Will be executed later
        public string[] fileDestBuffer; // Dest source name that are not prioritary
        public int fileBufferID = 0; // Used to determine index of both array (filebuffer and filedestbuffer)
        public int nbOfNonPrioFiles = 0;
    }
}
