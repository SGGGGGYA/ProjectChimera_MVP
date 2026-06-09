using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core;

[System.Serializable]
public class ShopData
{
    public List<string> shopItemIds = new List<string>();

    public void RefreshShop()
    {
        shopItemIds.Clear();

        var allItems = ItemDatabase.GetAll();
        var pool = new List<ItemDefinition>();
        var consumables = new List<ItemDefinition>();
        var equipments = new List<ItemDefinition>();

        foreach (var item in allItems)
        {
            if (item.category == ItemCategory.Material) continue;
            pool.Add(item);
            if (item.category == ItemCategory.Consumable)
                consumables.Add(item);
            else if (item.IsEquipment)
                equipments.Add(item);
        }

        Shuffle(consumables);
        int cCount = Mathf.Min(2, consumables.Count);
        for (int i = 0; i < cCount; i++)
            shopItemIds.Add(consumables[i].itemId);

        Shuffle(equipments);
        int eCount = Mathf.Min(1, equipments.Count);
        for (int i = 0; i < eCount; i++)
            shopItemIds.Add(equipments[i].itemId);

        int target = RandomProvider.Current.Range(4, 7);
        Shuffle(pool);
        foreach (var item in pool)
        {
            if (shopItemIds.Count >= target) break;
            if (!shopItemIds.Contains(item.itemId))
                shopItemIds.Add(item.itemId);
        }
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = RandomProvider.Current.Range(i, list.Count);
            T tmp = list[i];
            list[i] = list[r];
            list[r] = tmp;
        }
    }
}
