using System;

public static class helper {


    public static string SizeSuffix(Int64 value,int decimalPlaces = 1) {
        string[ ] SizeSuffixes = { "bytes","KB","MB","GB","TB","PB","EB","ZB","YB" };
        if (decimalPlaces < 0) {
            throw new ArgumentOutOfRangeException("decimalPlaces");
        }

        if (value < 0) {
            return"-" + SizeSuffix(-value,decimalPlaces);
        }

        if (value == 0) {
            return string.Format("{0:n" + decimalPlaces + "} bytes",0);
        }

        int mag = (int)Math.Log(value,1024);

        decimal adjustedSize = (decimal)value / (1L << (mag * 10));


        if (Math.Round(adjustedSize,decimalPlaces) >= 1000) {
            mag          += 1;
            adjustedSize /= 1024;
        }

        return string.Format("{0:n" + decimalPlaces + "} {1}",adjustedSize,SizeSuffixes[ mag ]);


    }
    public static string ConvertBytesToKbPerSecond(long bytesPerSecond)
    {
        const long bytesInKB = 1024;
        const long bytesInMB = bytesInKB * 1024;

        double speed;

        if (bytesPerSecond >= bytesInMB)
        {
            // Convert to MB/s
            speed = (double)bytesPerSecond / bytesInMB;
            return $"{speed:F2} MB/s";
        }

        // Display in KB/s
        speed = (double)bytesPerSecond / bytesInKB;
        return $"{speed:F2} KB/s";
    }

    
    //button
 
    
}