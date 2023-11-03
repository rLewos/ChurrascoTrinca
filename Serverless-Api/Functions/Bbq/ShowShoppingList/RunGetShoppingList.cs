using Domain.Entities;
using Domain.Repositories;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Domain.Services;

namespace Serverless_Api
{
	public partial class RunGetShoppingList
	{
		private readonly Person _user;
		private readonly IPersonService _personService;
		private readonly IBbqService _bbqService;

		public RunGetShoppingList(Person user, IPersonService personService, IBbqService bbqService) {
			_user = user;
			_bbqService = bbqService;
			_personService = personService;
		}

		[Function(nameof(RunGetShoppingList))]
		public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "churras/{id}/shoppinglist")] HttpRequestData req, string id)
		{
			Bbq? bbq = null;
			try
			{
				if (string.IsNullOrEmpty(id))
					throw new ArgumentNullException("Bbq id is null.");

				bbq = await _bbqService.GetShoppingListByBbqId(_user.Id, id);
			}
			catch (Exception e)
			{

				return req.CreateResponse(System.Net.HttpStatusCode.NoContent);
			}

			return await req.CreateResponse(System.Net.HttpStatusCode.OK, 
				new { ShoppingList = bbq.ShoppingL, 
					TotalMeat = bbq.TotalMeat.ToString() + "KG", 
					TotalVeg = bbq.TotalVeg.ToString() + "KG"
				});
		}
	}
}
