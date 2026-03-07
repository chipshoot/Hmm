namespace Hmm.ServiceApi.DtoEntity.Utility
{
    public class ApiGeoAddress
    {
        public string Street { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string Country { get; set; }

        public string ZipCode { get; set; }

        public string FormattedAddress { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }
    }
}
