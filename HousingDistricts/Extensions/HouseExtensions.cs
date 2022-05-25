
using TShockAPI;
using HousingDistricts.Extensions;
using System;

namespace HousingDistricts.Extensions
{
    public static class HouseExtensions
    {
        public static HPlayer GetHousePlayer(this TSPlayer player)
        {
            for (int i = 0; i < HousingDistricts.HPlayers.Count; i++)
            {
                var Hply = HousingDistricts.HPlayers[i];
                if (Hply.Index == player.Index) return Hply;
            }
            return new HPlayer();
        }
       
        public static House InAreaHouse(this TSPlayer ply)
        {
            int x = ply.TileX;
            int y = ply.TileY;
            for (int h = 0; h < HousingDistricts.Houses.Count; h++)
            {
                var house = HousingDistricts.Houses[h];
                if (x >= house.HouseArea.Left && x < house.HouseArea.Right &&
                    y >= house.HouseArea.Top && y < house.HouseArea.Bottom)
                {
                    return house;
                }
            }
            return null;
        }

        public static bool OwnsHouse(this TSPlayer ply)
        {
            bool isAdmin = false;
            House house = null;
            try { isAdmin = ply.Group.HasPermission("house.root"); house = ply.InAreaHouse(); }
            catch { }

            if (ply.Account.ID != 0 && house != null)
            {
                try
                {
                    if (house.Owners.Contains(ply.Account.ID.ToString()) || isAdmin) return true;
                    else return false;
                }
                catch (Exception ex)
                {
                    TShock.Log.Error(ex.ToString());
                    return false;
                }
            }
            return false;
        }
    }
}
