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
            var memberRole = context.Roles.Where(m => m.Name == "Buyer").FirstOrDefault();
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
        [Authorize(Roles = new[] { "Admin" })]
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

        [Authorize(Roles = new[] { "Manager" })]
        public async Task<UserData> AddCourierAsync(
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
            var memberRole = context.Roles.Where(m => m.Name == "Courier").FirstOrDefault();
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

        [Authorize(Roles = new[] { "Manager" })]
        public async Task<Courier> AddCourierProfileAsync(
           CourierInput input,
           [Service] FoodDeliveryContext context)
        {
            // EF
            var kurir = new Courier
            {
                CourierName = input.CourierName,
                PhoneNumber = input.Phone,
                UserId = input.userId
            };

            var ret = context.Couriers.Add(kurir);
            await context.SaveChangesAsync();

            return ret.Entity;
        }
        public async Task<Courier> UpdateCourierAsync(
            CourierInput input,
            [Service] FoodDeliveryContext context)
        {
            var kurir = context.Couriers.Where(o => o.Id == input.Id).FirstOrDefault();
            if (kurir != null)
            {
                kurir.CourierName = input.CourierName;
                kurir.PhoneNumber = input.Phone;

                context.Couriers.Update(kurir);
                await context.SaveChangesAsync();
            }

            return await Task.FromResult(kurir);
        }

        public async Task<User> DeleteCourierByIdAsync(
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
        //Delete CourierProfile
        public async Task<Courier> DeleteCourierProfileAsync(
            int id,
            [Service] FoodDeliveryContext context)
        {
            var kurir = context.Couriers.Where(o => o.Id == id).FirstOrDefault();
            if (kurir != null)
            {
                context.Couriers.Remove(kurir);
                await context.SaveChangesAsync();
            }

            return await Task.FromResult(kurir);
        }


    }
}