
using System.Linq;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.AspNetCore.Authorization;

using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using FoodService.Models;

namespace FoodService.GraphQL
{
    public class Query
    {
        [Authorize(Roles = new[] { "Manager", "Buyer" })]
        public IQueryable<Food> ViewFoods([Service] FoodDeliveryContext context) =>
            context.Foods;
    }
}
