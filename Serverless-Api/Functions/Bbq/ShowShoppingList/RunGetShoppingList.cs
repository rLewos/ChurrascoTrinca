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
			Person? person = await _personService.GetAsync(_user.Id);
			if (person == null)
				throw new NullReferenceException("Person not found.");

			if (!person.IsCoOwner)
				throw new Exception("Person is not 'CoOwner'");

			Bbq? bbq = await _bbqService.GetAsync(id);
			if (bbq == null)
				throw new Exception("Bbq not found.");

			return await req.CreateResponse(System.Net.HttpStatusCode.OK, 
				new { ShoppingList = bbq.ShoppingL, 
					TotalMeat = bbq.TotalMeat.ToString() + "KG", 
					TotalVeg = bbq.TotalVeg.ToString() + "KG"
				});
		}
	}
}
