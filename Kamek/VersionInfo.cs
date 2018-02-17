using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class VersionInfo
{
    public VersionInfo()
    {
        _mappers["default"] = new AddressMapper();
    }

    public VersionInfo(string path)
    {
        var commentRegex = new Regex(@"^\s*#");
        var emptyLineRegex = new Regex(@"^\s*$");
        var sectionRegex = new Regex(@"^\s*\[([a-zA-Z0-9_.]+)\]$");
        var extendRegex = new Regex(@"^\s*extend ([a-zA-Z0-9_.]+)\s*(#.*)?$");
        var mappingRegex = new Regex(@"^\s*([a-fA-F0-9]{8})-((?:[a-fA-F0-9]{8})|\*)\s*:\s*([-+])0x([a-fA-F0-9]+)\s*(#.*)?$");
        String currentVersionName = null;
        AddressMapper currentVersion = null;

        foreach (var line in File.ReadAllLines(path))
        {
            if (emptyLineRegex.IsMatch(line))
                continue;
            if (commentRegex.IsMatch(line))
                continue;

            var match = sectionRegex.Match(line);
            if (match.Success)
            {
                // New version
                currentVersionName = match.Groups[1].Value;
                if (_mappers.ContainsKey(currentVersionName))
                    throw new InvalidDataException(string.Format("versions file contains duplicate version name {0}", currentVersionName));

                currentVersion = new AddressMapper();
                _mappers[currentVersionName] = currentVersion;
                continue;
            }

            if (currentVersion != null)
            {
                // Try to associate something with the current version
                match = extendRegex.Match(line);
                if (match.Success)
                {
                    var baseName = match.Groups[1].Value;
                    if (!_mappers.ContainsKey(baseName))
                        throw new InvalidDataException(string.Format("version {0} extends unknown version {1}", currentVersionName, baseName));
                    if (currentVersion.Base != null)
                        throw new InvalidDataException(string.Format("version {0} already extends a version", currentVersionName));

                    currentVersion.Base = _mappers[baseName];
                    continue;
                }

                match = mappingRegex.Match(line);
                if (match.Success)
                {
                    uint startAddress, endAddress;
                    int delta;

                    startAddress = uint.Parse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
                    if (match.Groups[2].Value == "*")
                        endAddress = 0xFFFFFFFF;
                    else
                        endAddress = uint.Parse(match.Groups[2].Value, System.Globalization.NumberStyles.HexNumber);

                    delta = int.Parse(match.Groups[4].Value, System.Globalization.NumberStyles.HexNumber);
                    if (match.Groups[3].Value == "-")
                        delta = -delta;

                    currentVersion.AddMapping(startAddress, endAddress, delta);
                    continue;
                }
            }

            Console.WriteLine("unrecognised line in versions file: {0}", line);
        }
    }

    private Dictionary<string, AddressMapper> _mappers = new Dictionary<string, AddressMapper>();
    public IReadOnlyDictionary<string, AddressMapper> Mappers { get { return _mappers; } }
}
