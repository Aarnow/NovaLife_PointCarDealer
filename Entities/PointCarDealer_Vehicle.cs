using SQLite;

namespace PointCarDealer.Entities
{
    public class PointCarDealer_Vehicle : ModKit.ORM.ModEntity<PointCarDealer_Vehicle>
    {
        [AutoIncrement][PrimaryKey] public int Id { get; set; }
        public int ModelId { get; set; }
        public string ModelName { get; set; }
        public double Price { get; set; }
        public bool IsBuyable { get; set; }
        public bool IsResellable { get; set; }
        public string Serigraphie {  get; set; }
        public PointCarDealer_Vehicle()
        {
        }
    }
}
