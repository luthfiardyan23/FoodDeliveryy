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
using Microsoft.AspNetCore.Mvc;
using OrderService.Models;
using OrderService.Models;
using Microsoft.EntityFrameworkCore;

namespace OrderService.GraphQL
{
    public class Mutation
    {
        [Authorize(Roles = new[] { "Buyer" })]
        public async Task<OrderData> AddOrderAsync(
        OrderData input,
        ClaimsPrincipal claimsPrincipal,
        [Service] FoodDeliveryContext context)
        {
            using var transaction = context.Database.BeginTransaction();
            var userName = claimsPrincipal.Identity.Name;
            try
            {
                var user = context.Users.Where(o => o.Username == userName).FirstOrDefault();
                if (user != null)
                {
                    // EF
                    var order = new Order
                    {
                        Code = Guid.NewGuid().ToString(), // generate random chars using GUID
                        UserId = user.Id,
                        CourierId = input.CourierId
                    };

                    foreach (var item in input.Details)
                    {
                        var detail = new OrderDetail
                        {
                            OrderId = order.Id,
                            FoodId = item.FoodId,
                            Quantity = item.Quantity
                        };
                        order.OrderDetails.Add(detail);
                    }
                    context.Orders.Add(order);
                    context.SaveChanges();
                    await transaction.CommitAsync();

                    //input.Id = order.Id;
                    //input.Code = order.Code;
                }
                else
                    throw new Exception("user was not found");
            }
            catch (Exception err)
            {
                transaction.Rollback();
            }

            return input;
        }

        [Authorize(Roles = new[] { "Manager" })]
        public async Task<OrderData> UpdateOrderAsync(
        OrderData input,
        [Service] FoodDeliveryContext context)
        {
            var user = context.Orders.Where(o => o.Id == input.Id).FirstOrDefault();
            if (user != null)
            {
                user.Code = Guid.NewGuid().ToString();
                user.UserId = input.userId;
                user.CourierId = input.CourierId;

                foreach (var item in input.Details)
                {
                    var detail = new OrderDetail
                    {
                        OrderId = user.Id,
                        FoodId = item.FoodId,
                        Quantity = item.Quantity
                    };
                    user.OrderDetails.Add(detail);
                }
                context.Orders.Update(user);
                await context.SaveChangesAsync();
            }
            return input;
        }

        [Authorize(Roles = new[] { "MANAGER" })]
        public async Task<Order> DeleteOrderByIdAsync(
            int id,
            [Service] FoodDeliveryContext context)
        {
            var order = context.Orders.Where(o => o.Id == id).Include(o => o.OrderDetails).FirstOrDefault();
            if (order != null)
            {
                context.Orders.Remove(order);
                await context.SaveChangesAsync();
            }

            return await Task.FromResult(order);
        }

        [Authorize]
        public async Task<Courier> AddCourierAsync(
            RegisterCourier input,
            [Service] FoodDeliveryContext context)
        {
            var courier = new Courier
            {
                CourierName = input.CourierName,
                PhoneNumber = input.PhoneNumber,
            };

            var ret = context.Couriers.Add(courier);
            await context.SaveChangesAsync();
            return ret.Entity;

        }

        [Authorize(Roles = new[] { "MANAGER" })]
        public async Task<Courier> UpdateCourierAsync(
            RegisterCourier input,
            [Service] FoodDeliveryContext context)
        {
            var courier = context.Couriers.Where(o => o.Id == input.Id).FirstOrDefault();
            if (courier != null)
            {
                courier.CourierName = input.CourierName;
                courier.PhoneNumber = input.PhoneNumber;

                context.Couriers.Update(courier);
                await context.SaveChangesAsync();
            }
            return await Task.FromResult(courier);
        }

    }
}