using Catalog.Api.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IItemsRepository
  {
    Task<Item> GetItemAsync(Guid id);
    Task<IEnumerable<Item>> GetItemsAsync();
    Task CreateItemAsync(Item item);
    Task UpdateItemAsync(Item item);
    Task DeleteItemAsync(Guid id);
  }