﻿using System;
using System.Collections.Generic;

namespace Pssg.Interfaces.IcbcModels
{
    public class DriverDetails
    {
        public int LicenceNumber { get; set; }
        public DateTime LicenceExpiryDate { get; set; }
        public int LicenceClass { get; set; }
        public string MasterStatusCode { get; set; }
        public List<Restrictions> Restrictions { get; set; }
        public List<ExpandedStatuses> ExpandedStatuses { get; set; }
        public List<Medicals> Medicals { get; set; }
    }
}

