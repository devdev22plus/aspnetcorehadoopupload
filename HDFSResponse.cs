using System;
using System.Collections.Generic;

namespace HDFSResponse
{
    public class Redirection
    {
        public string Location;

        public bool URLSwitch(IEnumerable<KeyValuePair<string, string>> URLSwitch)
        {
            // Console.WriteLine();
            // Console.Write($"URLSwitch {Location} > ");
            foreach(var l in URLSwitch)
            {
                // Console.WriteLine($"replace {l.Key} > {l.Value}");
                Location = Location.Replace(l.Key, l.Value);
            }
            // Console.Write($"{Location}");

            return true;
        }
    }
    
    public class DeleteFile
    {
        public bool boolean;
    }

    public class FileStatus
    {
        public long accessTime;
        public long blockSize;
        public int childrenNum;
        public int fileId;
        public string group;
        //in bytes, zero for directories
        public long length;
        public long modificationTime;
        public string owner;
        public string pathSuffix;
        public string permission;
        public int replication;
        public int storagePolicy;
        public bool snapshotEnabled;
        public enum EM_FILETYPE {FILE, DIRECTORY, SYMLINK}
        public string type;
    }

    public class FileStatuses
    {
        public FileStatus[] fileStatus;

        public FileStatuses fileStatuses;
    }

    public class FileGetStatus
    {
        public FileStatus fileStatus;
    }

    public class FileChecksum
    {
        public string algorithm;
        public string bytes;
        public long length;

        public FileChecksum fileChecksum;
    }
}
