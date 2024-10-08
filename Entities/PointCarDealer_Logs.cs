using SQLite;

namespace PointCarDealer.Entities
{
    internal class PointCarDealer_Logs : ModKit.ORM.ModEntity<PointCarDealer_Logs>
    {
        [AutoIncrement][PrimaryKey] public int Id { get; set; }
        public int CarDealerId { get; set; }
        public int CharacterId { get; set; }
        public string CharacterFullName { get; set; }
        public int ModelId { get; set; }
        public int BizId { get; set; }
        public bool IsPurchase { get; set; }
        public double Price { get; set; }
        public int CreatedAt { get; set; }

        public PointCarDealer_Logs()
        {
        }
    }
}
