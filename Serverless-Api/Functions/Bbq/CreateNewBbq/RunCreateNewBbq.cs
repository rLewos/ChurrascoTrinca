using Eveneum;
using System.Net;
using CrossCutting;
using Domain.Events;
using Domain.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Domain.Repositories;
using Domain;

namespace Serverless_Api
{
    public partial class RunCreateNewBbq
    {
        private readonly Person _user;
        private readonly SnapshotStore _snapshots;
        
        private readonly IPersonRepository _personRepository;
        private readonly IBbqRepository _bbqRepository;

		public RunCreateNewBbq(IPersonRepository personRepository, IBbqRepository bbqRepository, SnapshotStore snapshots, Person user)
		{
			_user = user;
			_snapshots = snapshots;
            _personRepository = personRepository;
            _bbqRepository = bbqRepository;			
		}

		[Function(nameof(RunCreateNewBbq))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "churras")] HttpRequestData req)
        {
            var input = await req.Body<NewBbqRequest>();

            if (input == null)
                return await req.CreateResponse(HttpStatusCode.BadRequest, "input is required.");

            var churras = new Bbq();
            churras.Apply(new ThereIsSomeoneElseInTheMood(Guid.NewGuid(), input.Date, input.Reason, input.IsTrincasPaying));
            await _bbqRepository.SaveAsync(churras);

            var churrasSnapshot = churras.TakeSnapshot();

            // Invite Moderators
            var Lookups = await _snapshots.AsQueryable<Lookups>("Lookups").SingleOrDefaultAsync();
            try
            {
                foreach (var personId in Lookups.ModeratorIds)
                {
                    Person? person = await _personRepository.GetAsync(personId);
					var @event = new PersonHasBeenInvitedToBbq(churras.Id, churras.Date, churras.Reason);                    
                    person.Apply(@event);
                    
                    await _personRepository.SaveAsync(person);
                }
            }
            catch (Exception e)
            {

                throw;
            }


            return await req.CreateResponse(HttpStatusCode.Created, churrasSnapshot);
        }
    }
}
