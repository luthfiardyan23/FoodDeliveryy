using FoodService.Models;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate;
using System;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;

using HotChocolate.AspNetCore.Authorization;
using System.Security.Claims;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using FoodService.Models;
using FoodService.GraphQL;
using FoodService.Models;

namespace FoodService.GraphQL
{
    public class Mutation
    {
        [Authorize(Roles = new[] { "MANAGER" })]
        public async Task<Food> AddProductAsync(
            FoodInput input,
            [Service] FoodDeliveryContext context)
        {
            
            // EF
            var product = new Food
            {
                Name = input.Name,
                Stock = input.Stock,
                Price = input.Price,
                Created = DateTime.Now
            };

            var ret = context.Foods.Add(product);
            await context.SaveChangesAsync();

            return ret.Entity;
        }
        public async Task<Food> GetProductByIdAsync(
            int id,
            [Service] FoodDeliveryContext context)
        {
            var product = context.Foods.Where(o => o.Id ==id).FirstOrDefault();

            return await Task.FromResult(product);
        }
        [Authorize(Roles = new[] { "MANAGER" })]
        public async Task<Food> UpdateProductAsync(
            FoodInput input,
            [Service] FoodDeliveryContext context)
        {
            var food = context.Foods.Where(o => o.Id == input.Id).FirstOrDefault();
            if (food != null)
            {
                food.Name = input.Name;
                food.Stock = input.Stock;
                food.Price = input.Price;

                context.Foods.Update(food);
                await context.SaveChangesAsync();
            }


            return await Task.FromResult(food);
        }

        [Authorize(Roles = new[] { "MANAGER" })]
        public async Task<Food> DeleteProductByIdAsync(
            int id,
            [Service] FoodDeliveryContext context)
        {
            var food = context.Foods.Where(o => o.Id == id).FirstOrDefault();
            if (food != null)
            {
                context.Foods.Remove(food);
                await context.SaveChangesAsync();
            }


            return await Task.FromResult(food);
        }

        

    }
}
