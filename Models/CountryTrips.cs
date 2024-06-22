using System;
using System.Collections.Generic;

namespace TripsAPI.Models
{
    public partial class CountryTrip
    {
        public int IdCountry { get; set; }
        public int IdTrip { get; set; }

        public virtual Country Country { get; set; }
        public virtual Trip Trip { get; set; }
    }
}