namespace Surveillance_MVP.Models.Lookups
{
    public class Country
    {

        public int Id { get; set; }
        public string Code { get; set; }   // ISO code
        public string Name { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
