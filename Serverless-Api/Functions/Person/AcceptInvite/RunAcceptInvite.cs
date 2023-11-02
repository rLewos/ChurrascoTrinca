using Domain.Events;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Serverless_Api
{
	public partial class RunAcceptInvite
	{
		private readonly Person _user;
		private readonly IPersonRepository _personRepository;
		private readonly IBbqRepository _bbqRepository;
		public RunAcceptInvite(IPersonRepository personRepository, IBbqRepository bbqRepository, Person user)
		{
			_user = user;
			_personRepository = personRepository;
			_bbqRepository = bbqRepository;

		}

		[Function(nameof(RunAcceptInvite))]
		public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "put", Route = "person/invites/{inviteId}/accept")] HttpRequestData req, string inviteId)
		{
			var answer = await req.Body<InviteAnswer>();
			var person = await _personRepository.GetAsync(_user.Id);

			try
			{
				if (person == null)
					throw new Exception("Person not found.");

				person.Apply(new InviteWasAccepted { InviteId = inviteId, IsVeg = answer.IsVeg, PersonId = person.Id });
				await _personRepository.SaveAsync(person);

				//implementar efeito do aceite do convite no churrasco
				Bbq? bbq = await _bbqRepository.GetAsync(inviteId);
				if (bbq == null)
					throw new Exception("Bbq not found.");

				var @event = new PersonHasConfirmed { BbqID = bbq.Id, PersonID = person.Id, IsVeg = answer.IsVeg };
				bbq.Apply(@event);
				await _bbqRepository.SaveAsync(bbq);
			}
			catch (Exception e)
			{

				throw;
			}


			return await req.CreateResponse(System.Net.HttpStatusCode.OK, person.TakeSnapshot());
		}
	}
}
