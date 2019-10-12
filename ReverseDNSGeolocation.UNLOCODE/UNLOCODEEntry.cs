namespace ReverseDNSGeolocation.UNLOCODE
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    // https://www.unece.org/fileadmin/DAM/cefact/locode/unlocode_manual.pdf
    // Columns: Change,Country,Location,Name,NameWoDiacritics,Subdivision,Status,Function,Date,IATA,Coordinates,Remarks
    // Example: ,AF,BAG,Bagram,Bagram,PAR,RL,--3-----,0307,,3457N 06915E,
    public class UNLOCODEEntry
    {
        public string Change { get; set; }
        
        public string Country { get; set; }
        
        public string Location { get; set; }

        public string Name { get; set; }
        
        public string NameWithoutDiacritics { get; set; }

        public string Subdivision { get; set; }

        public string Status { get; set; }

        public string Function { get; set; }

        public string Date { get; set; }
        
        public string IATA { get; set; }
        
        public string Coordinates { get; set; }

        public string Remarks { get; set; }
    }
}
