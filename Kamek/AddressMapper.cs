using System;
using System.Collections.Generic;

public class AddressMapper
{
    public AddressMapper Base = null;


    struct Mapping
    {
        public uint start, end;
        public int delta;
        
        public bool Overlaps(Mapping other)
        {
            return (end >= other.start) && (start <= other.end);
        }

        public override string ToString()
        {
            return string.Format("{0:8X}-{1:8X}: {2}0x{3:X}", start, end, (delta >= 0) ? '+' : '-', Math.Abs(delta));
        }
    }

    private List<Mapping> _mappings = new List<Mapping>();

    public void AddMapping(uint start, uint end, int delta)
    {
        if (start > end)
            throw new ArgumentException(string.Format("cannot map {0:8X}-{1:8X} as start is higher than end", start, end));

        var newMapping = new Mapping() { start = start, end = end, delta = delta };

        foreach (var mapping in _mappings)
        {
            if (mapping.Overlaps(newMapping))
                throw new ArgumentException(string.Format("new mapping {0} overlaps with existing mapping {1}", newMapping, mapping));
        }

        _mappings.Add(newMapping);
    }


    public uint Remap(uint input)
    {
        if (Base != null)
            input = Base.Remap(input);

        foreach (var mapping in _mappings)
        {
            if (input >= mapping.start && input <= mapping.end)
                return (uint)(input + mapping.delta);
        }

        return input;
    }
}
