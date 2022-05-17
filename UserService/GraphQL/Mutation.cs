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
using UserService.GraphQL;
using UserService.Models;
using Microsoft.EntityFrameworkCore;

namespace UserService.GraphQL
{
    public class Mutation
    {
        public async Task<UserData> RegisterUserAsync(
            RegisterUser input,
            [Service] FoodDeliveryContext context)
        {
            var user = context.Users.Where(o => o.Username == input.UserName).FirstOrDefault();
            if (user != null)
            {
                return await Task.FromResult(new UserData());
            }
            var newUser = new User
            {
                FullName = input.FullName,
                Email = input.Email,
                Username = input.UserName,
                Password = BCrypt.Net.BCrypt.HashPassword(input.Password) // encrypt password
            };
            var memberRole = context.Roles.Where(m => m.Name == "BUYER").FirstOrDefault();
            if (memberRole == null)
                throw new Exception("Invalid Role");
            var userRole = new UserRole
            {
                RoleId = memberRole.Id,
                UserId = newUser.Id
            };
            newUser.UserRoles.Add(userRole);
            // EF
            var ret = context.Users.Add(newUser);
            await context.SaveChangesAsync();

            return await Task.FromResult(new UserData
            {
                Id = newUser.Id,
                Username = newUser.Username,
                Email = newUser.Email,
                FullName = newUser.FullName
            });
        }
        public async Task<UserToken> LoginAsync(
            LoginUser input,
            [Service] IOptions<TokenSettings> tokenSettings, // setting token
            [Service] FoodDeliveryContext context) // EF
        {
            var user = context.Users.Where(o => o.Username == input.Username).FirstOrDefault();
            if (user == null)
            {
                return await Task.FromResult(new UserToken(null, null, "Username or password was invalid"));
            }
            bool valid = BCrypt.Net.BCrypt.Verify(input.Password, user.Password);
            if (valid)
            {
                // generate jwt token
                var securitykey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenSettings.Value.Key));
                var credentials = new SigningCredentials(securitykey, SecurityAlgorithms.HmacSha256);

                // jwt payload
                var claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.Name, user.Username));

                var userRoles = context.UserRoles.Where(o => o.UserId == user.Id).ToList();
                foreach (var userRole in userRoles)
                {
                    var role = context.Roles.Where(o => o.Id == userRole.RoleId).FirstOrDefault();
                    if (role != null)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role.Name));
                    }
                }

                var expired = DateTime.Now.AddHours(3);
                var jwtToken = new JwtSecurityToken(
                    issuer: tokenSettings.Value.Issuer,
                    audience: tokenSettings.Value.Audience,
                    expires: expired,
                    claims: claims, // jwt payload
                    signingCredentials: credentials // signature
                );

                return await Task.FromResult(
                    new UserToken(new JwtSecurityTokenHandler().WriteToken(jwtToken),
                    expired.ToString(), null));
                //return new JwtSecurityTokenHandler().WriteToken(jwtToken);
            }

            return await Task.FromResult(new UserToken(null, null, Message: "Username or password was invalid"));
        }
        [Authorize]
        public async Task<User> UpdateUserAsync(
            UserData input,
            [Service] FoodDeliveryContext context)
        {
            var user = context.Users.Where(o => o.Id == input.Id).FirstOrDefault();
            if (user != null)
            {
                user.FullName = input.FullName;
                user.Username = input.Username;
                user.Email = input.Email;

                context.Users.Update(user);
                await context.SaveChangesAsync();
            }


            return await Task.FromResult(user);
        }
        [Authorize(Roles = new[] { "ADMIN" })]
        public async Task<User> DeleteUserByIdAsync(
            int id,
            [Service] FoodDeliveryContext context)
        {
            var user = context.Users.Where(o => o.Id == id).Include(o => o.UserRoles).FirstOrDefault();
            if (user != null)
            {
                context.Users.Remove(user);
                await context.SaveChangesAsync();
            }


            return await Task.FromResult(user);
        }
        [Authorize]
        public async Task<User> ChangePasswordAsync(
            ChangePassword input,
            [Service] FoodDeliveryContext context)
        {
            var user = context.Users.Where(o => o.Id == input.Id).FirstOrDefault();
            if (user != null)
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(input.Password);

                context.Users.Update(user);
                await context.SaveChangesAsync();
            }

            return await Task.FromResult(user);
        }

        [Authorize]
        public async Task<Profile> AddProfileAsync(
            ProfilesInput input,
            [Service] FoodDeliveryContext context)
        {
            var profile = new Profile
            {
                UserId = input.UserId,
                Name = input.Name,
                Addres = input.Address,
                City = input.City,
                Phone = input.Phone
            };



            var ret = context.Profiles.Add(profile);

            await context.SaveChangesAsync();

            return ret.Entity;

        }

        //[Authorize]
        //public async Task<Order> AddOrderAsync(
        //    Order input,
        //    ClaimsPrincipal claimsPrincipal,
        //    [Service] ProductQLContext context)
        //{
        //    using var transaction = context.Database.BeginTransaction();
        //    var userName = claimsPrincipal.Identity.Name;

        //    try
        //    {
        //        var user = context.Users.Where(o => o.Username == userName).FirstOrDefault();
        //        if (user != null)
        //        {
        //            // EF
        //            var order = new Order
        //            {
        //                Code = Guid.NewGuid().ToString(), // generate random chars using GUID
        //                UserId = user.Id
        //            };

        //            foreach (var item in input.Details)
        //            {
        //                var detial = new OrderDetail
        //                {
        //                    OrderId = order.Id,
        //                    ProductId = item.ProductId,
        //                    Quantity = item.Quantity
        //                };
        //                order.OrderDetails.Add(detial);            
        //            }
        //            context.Orders.Add(order);
        //            context.SaveChanges();
        //            await transaction.CommitAsync();

        //            input.Id = order.Id;
        //            input.Code = order.Code;
        //        }
        //        else
        //            throw new Exception("user was not found");
        //    }
        //    catch(Exception err)
        //    {
        //        transaction.Rollback();
        //    }



        //    return input;
        //}

    }
}