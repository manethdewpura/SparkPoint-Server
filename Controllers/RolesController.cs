//using MongoDB.Driver;
//using SparkPoint_Server.Models;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Web.Http;
//using SparkPoint_Server.Helpers;

//namespace SparkPoint_Server.Controllers
//{
//    [RoutePrefix("api/roles")]
//    public class RolesController : ApiController
//    {
//        private readonly IMongoCollection<Role> _rolesCollection;

//        public RolesController()
//        {
//            var dbContext = new MongoDbContext();
//            _rolesCollection = dbContext.GetCollection<Role>("Roles");
//        }

//        [HttpPost]
//        [Route("")]
//        public async Task<IHttpActionResult> CreateRole(Role role)
//        {
//            if (string.IsNullOrWhiteSpace(role.Name))
//            {
//                return BadRequest("Role name is required.");
//            }
//            // Assign next available integer ID if not provided
//            if (role.Id == 0)
//            {
//                var maxRole = await _rolesCollection.Find(_ => true)
//                    .SortByDescending(r => r.Id)
//                    .Limit(1)
//                    .FirstOrDefaultAsync();
//                role.Id = (maxRole != null) ? maxRole.Id + 1 : 1;
//            }
//            await _rolesCollection.InsertOneAsync(role);
//            return Ok(role);
//        }
//    }
//}
