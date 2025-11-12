
using System.Globalization;

namespace MIT_task;

class Program
{
    public static void Main(string[] args)
    {
        try
        {
            int fingers = 40;
            double pipeDiamMM = 254; //mm
            double pipeRadMM = pipeDiamMM / 2.0;

            // first retrieve the path of the data document /Users/mateigrosu/Downloads/data.txt
            string inPath = RetrievePath(args);

            // check it exists
            if (!File.Exists(inPath))
            {
                Console.WriteLine("File not found");
                return;
            }

            // extract the data from the path
            var data = ExtractData(inPath);
            Console.WriteLine($"Loaded {data.Count} rows × {data[0].Length} cols");

            // calculate the offset
            var (dx,dy) = CalculateOffset(data, fingers, pipeRadMM);
            Console.WriteLine($"dx = {dx:F3} mm, dy = {dy:F3} mm");
            
            // return the corrected data
            var corrected = Correction(data, fingers, dx, dy);
            
            // save in csv
            SaveFile(corrected, inPath);

        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    /// <summary>
    /// Get data file path from command line 
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    static string RetrievePath(string[] args)
    {
        if (args.Length > 0)
            return args[0];

        Console.Write("Enter input file path: ");
        return Console.ReadLine()?.Trim() ?? "";
    }

    /// <summary>
    /// Read the separated data from the file
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    static List<double[]> ExtractData(string path)
    {
        var rows =  new List<double[]>();

        foreach (var line in File.ReadAllLines(path))
        {
            var values = line.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(v => double.Parse(v, CultureInfo.InvariantCulture))
                .ToArray();

            rows.Add(values);
        }

        return rows;
    }
    
    /// <summary>
    /// Calculates the offset from the ideal center position.
    /// Each finger is at a different angle around the circle.
    /// If pipe shifts right, right-side fingers measure less, left-side measures more
    /// Method uses all 40 fingers to find the average shift pattern
    /// </summary>
    /// <param name="data"></param>
    /// <param name="fingers"></param>
    /// <param name="pipeRadMm"></param>
    /// <returns></returns>
    static (double dx, double dy) CalculateOffset(List<double[]> data, int fingers, double pipeRadMm)
    {
        // calculate the angle position of each finger
        double[] cos = new double[fingers];
        double[] sin = new double[fingers];
        for (int i = 0; i < fingers; i++)
        {
            double angle = i * 2 * Math.PI / fingers;
            cos[i] = Math.Cos(angle);
            sin[i] = Math.Sin(angle);
        }

        // collect data from all fingers about horizontal & vertical shift
        double sumCosY = 0, sumSinY = 0;
        double sumCos2 = 0, sumSin2 = 0;
        
        foreach (var row in data)
        {
            for (int i = 0; i < fingers; i++)
            {
                double diff = row[i] - pipeRadMm; // difference from perfect 
                
                // calculate the weight, how much does it interfere 
                sumCosY += cos[i] * diff;
                sumSinY += sin[i] * diff;
                sumCos2 += cos[i] * cos[i];
                sumSin2 += sin[i] * sin[i];
            }
        }

        // average shift from the cal
        double a = sumCosY / sumCos2;
        double b = sumSinY / sumSin2;

        // return offset, negative conversion
        double dx = -a;
        double dy = -b;

        return (dx, dy);
    }

    /// <summary>
    /// Fix measurements by reversing the offset.
    /// Add back what was lost or subtract what was gained due to the shift
    /// </summary>
    /// <param name="data"></param>
    /// <param name="fingers"></param>
    /// <param name="dx"></param>
    /// <param name="dy"></param>
    /// <returns></returns>
    static List<double[]> Correction(List<double[]> data, int fingers, double dx, double dy)
    {
        // calculate finger angles again
        var cos = new double[fingers];
        var sin = new double[fingers];
        for (int i = 0; i < fingers; i++)
        {
            double t = 2 * Math.PI * i / fingers;
            cos[i] = Math.Cos(t);
            sin[i] = Math.Sin(t);
        }

        // apply the correction to each measurment 
        var result = new List<double[]>(data.Count);
        foreach (var row in data)
        {
            var outRow = new double[fingers];
            for (int i = 0; i < fingers; i++)
            {
                // adjust based on angle and offset amount 
                outRow[i] = row[i] + dx * cos[i] + dy * sin[i];
            }
            result.Add(outRow);
        }
        return result;
    }
    
    /// <summary>
    /// Save corrected data to CSV
    /// </summary>
    /// <param name="corrected"></param>
    /// <param name="inPath"></param>
    private static void SaveFile(List<double[]> corrected, string inPath)
    {
        
        // create the Output path 
        string directory = Path.GetDirectoryName(inPath) ?? "";
        string fileName = Path.GetFileNameWithoutExtension(inPath);
        string newPath = Path.Combine(directory, fileName + "_corrected.csv");
        
        
        // write the data, 5 digits, space separated 
        using var sw = new StreamWriter(newPath);
        foreach (var row in corrected)
            sw.WriteLine(string.Join(" ", row.Select(v => v.ToString("G5", CultureInfo.InvariantCulture))));
        
        Console.WriteLine($"\nSaved corrected file → {newPath}");
    }
}