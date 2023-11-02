using Domain.Entities;
using Domain.Repositories;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;

namespace Serverless_Api
{
	public partial class RunGetShoppingList
	{
		private readonly Person _user;
		private readonly IPersonRepository _personRepository;
		private readonly IBbqRepository _bbqRepository;

		public RunGetShoppingList(Person user, IBbqRepository bbqRepository, IPersonRepository personRepository) {
			_user = user;
			_personRepository = personRepository;
			_bbqRepository = bbqRepository;
		}

		[Function(nameof(RunGetShoppingList))]
		public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "churras/{id}/shoppinglist")] HttpRequestData req, string id)
		{
			Person? person = await _personRepository.GetAsync(_user.Id);
			if (person == null)
				throw new NullReferenceException("Person not found.");

			if (!person.IsCoOwner)
				throw new Exception("Person is not 'CoOwner'");

			Bbq? bbq = await _bbqRepository.GetAsync(id);
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
