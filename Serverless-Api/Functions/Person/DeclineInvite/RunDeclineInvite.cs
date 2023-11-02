using Domain;
using Eveneum;
using CrossCutting;
using Domain.Events;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using static Domain.ServiceCollectionExtensions;

namespace Serverless_Api
{
    public partial class RunDeclineInvite
    {
        private readonly Person _user;
        private readonly IPersonRepository _personRepository;
        private readonly IBbqRepository _bbqRepository;

        public RunDeclineInvite(Person user, IPersonRepository personRepository, IBbqRepository bbqRepository)
        {
            _user = user;
			_personRepository = personRepository;
            _bbqRepository = bbqRepository;
        }

        [Function(nameof(RunDeclineInvite))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "put", Route = "person/invites/{inviteId}/decline")] HttpRequestData req, string inviteId)
        {
            var person = await _personRepository.GetAsync(_user.Id);

            if (person == null)
                return req.CreateResponse(System.Net.HttpStatusCode.NoContent);

            person.Apply(new InviteWasDeclined { InviteId = inviteId, PersonId = person.Id });
            await _personRepository.SaveAsync(person);

            //Implementar impacto da recusa do convite no churrasco caso ele já tivesse sido aceito antes
            Bbq? bbq = await _bbqRepository.GetAsync(inviteId);
            var @event = new InviteWasDeclined() { InviteId = inviteId, PersonId = person.Id};
            bbq.Apply(@event);
            await _bbqRepository.SaveAsync(bbq);

            return await req.CreateResponse(System.Net.HttpStatusCode.OK, person.TakeSnapshot());
        }
    }
}
