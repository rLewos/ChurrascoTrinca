using CrossCutting;
using Domain.Entities;
using Domain.Events;
using Domain.Repositories;
using Domain.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Serverless_Api
{
    public partial class RunModerateBbq
    {
        private readonly SnapshotStore _snapshots;
		private readonly IPersonService _personService;
		private readonly IBbqService _bbqService;

		public RunModerateBbq(SnapshotStore snapshots, IBbqService bbqService, IPersonService personService)
        {
            _snapshots = snapshots;
			_bbqService = bbqService;
			_personService = personService;
        }

        [Function(nameof(RunModerateBbq))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "put", Route = "churras/{id}/moderar")] HttpRequestData req, string id)
        {
            var moderationRequest = await req.Body<ModerateBbqRequest>();

            try
            {
				var bbq = await _bbqService.GetAsync(id);
				if (bbq == null)
					throw new Exception("Bbq not found.");

				bbq.Apply(new BbqStatusUpdated(moderationRequest.GonnaHappen, moderationRequest.TrincaWillPay));

				var lookups = await _snapshots.AsQueryable<Lookups>("Lookups").SingleOrDefaultAsync();
				
				// Do not send invites when bbq will not happen.
                if (!(bbq.Status == BbqStatus.ItsNotGonnaHappen))
                {
                    // Sending invites
					foreach (var personId in lookups.PeopleIds)
					{
						var person = await _personService.GetAsync(personId);
						if (person != null && !lookups.ModeratorIds.Any(x => x.Equals(person.Id)))
						{
							var @event = new PersonHasBeenInvitedToBbq(bbq.Id, bbq.Date, bbq.Reason);
							person.Apply(@event);
							await _personService.SaveAsync(person);
						}
					}
                }
                else
                {
					// Reject all invites
					foreach (var personId in lookups.PeopleIds)
					{
						var person = await _personService.GetAsync(personId);
						Invite invite = person.Invites.FirstOrDefault(x => x.Id == bbq.Id);
						if (invite != null)
						{
							var @event = new InviteWasDeclined() { 
								PersonId = personId,
								InviteId = invite.Id
							};

							person.Apply(@event);
							await _personService.SaveAsync(person);
						}
					}
				}

				await _bbqService.SaveAsync(bbq);

                return await req.CreateResponse(System.Net.HttpStatusCode.OK, bbq.TakeSnapshot());
			}
			catch (Exception e)
            {

                throw e;
            }
        }
    }
}
