using CrossCutting;
using Domain.Entities;
using Domain.Events;
using Domain.Repositories;
using Eveneum;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Services
{
	public class BbqService : IBbqService
	{
		private readonly SnapshotStore _snapshots;
		private readonly IBbqRepository _bbqRepository;
		private readonly IPersonRepository _personRepository;
		public BbqService(SnapshotStore snapshotStore, IBbqRepository bbqRepository, IPersonRepository personRepository)
		{
			_bbqRepository = bbqRepository;
			_personRepository = personRepository;
			_snapshots = snapshotStore;
		}

		public async Task<List<object>> GetAllNotRejectedOrAvailable(string userId)
		{
			var snapshots = new List<object>();
			var moderator = await _personRepository.GetAsync(userId);
			foreach (var bbqId in moderator.Invites.Where(i => i.Date > DateTime.Now).Select(o => o.Id).ToList())
			{
				var bbq = await this.GetAsync(bbqId);
				if (bbq == null)
					throw new Exception("Bbq not found.");

				if (bbq.Status != BbqStatus.ItsNotGonnaHappen)
					snapshots.Add(bbq.TakeSnapshot());
			}

			return snapshots;
		}

		public async Task<Bbq?> GetAsync(string streamId)
		{
			return await _bbqRepository.GetAsync(streamId);
		}

		public async Task<StreamHeaderResponse> GetHeaderAsync(string streamId)
		{
			return await _bbqRepository.GetHeaderAsync(streamId);
		}

		public async Task<Bbq?> GetShoppingListByBbqId(string userId, string bbqId)
		{
			Person? person = await _personRepository.GetAsync(userId);
			if (person == null)
				throw new NullReferenceException("Person not found.");

			if (!person.IsCoOwner)
				throw new Exception("Person is not 'CoOwner'");

			Bbq? bbq = await this.GetAsync(bbqId);
			if (bbq == null)
				throw new Exception("Bbq not found.");

			return bbq;
		}

		public async Task<Bbq?> Moderate(string id, bool gonnaHappen, bool trincaWillPay)
		{
			var bbq = await this.GetAsync(id);
			if (bbq == null)
				throw new Exception("Bbq not found.");

			bbq.Apply(new BbqStatusUpdated(gonnaHappen, trincaWillPay));

			var lookups = await _snapshots.AsQueryable<Lookups>("Lookups").SingleOrDefaultAsync();

			// Do not send invites when bbq will not happen.
			if (!(bbq.Status == BbqStatus.ItsNotGonnaHappen))
				await SendInvites(bbq, lookups);
			else
				await RejectInvites(bbq, lookups);

			await this.SaveAsync(bbq);
			return bbq;
		}

		private async Task RejectInvites(Bbq? bbq, Lookups lookups)
		{
			// Reject all invites
			foreach (var personId in lookups.PeopleIds)
			{
				var person = await _personRepository.GetAsync(personId);
				Invite invite = person.Invites.FirstOrDefault(x => x.Id == bbq.Id);
				if (invite != null)
				{
					var @event = new InviteWasDeclined()
					{
						PersonId = personId,
						InviteId = invite.Id
					};

					person.Apply(@event);
					await _personRepository.SaveAsync(person);
				}
			}
		}

		private async Task SendInvites(Bbq? bbq, Lookups lookups)
		{
			// Sending invites
			foreach (var personId in lookups.PeopleIds)
			{
				var person = await _personRepository.GetAsync(personId);
				if (person != null && !lookups.ModeratorIds.Any(x => x.Equals(person.Id)))
				{
					var @event = new PersonHasBeenInvitedToBbq(bbq.Id, bbq.Date, bbq.Reason);
					person.Apply(@event);
					await _personRepository.SaveAsync(person);
				}
			}
		}

		public async Task SaveAsync(Bbq entity)
		{
			await _bbqRepository.SaveAsync(entity);
		}

		public async Task<Bbq?> CreateNew(DateTime date, string reason, bool isTrincasPaying)
		{
			var churras = new Bbq();
			churras.Apply(new ThereIsSomeoneElseInTheMood(Guid.NewGuid(), date, reason, isTrincasPaying));
			await this.SaveAsync(churras);

			// Invite Moderators
			var Lookups = await _snapshots.AsQueryable<Lookups>("Lookups").SingleOrDefaultAsync();
			foreach (var personId in Lookups.ModeratorIds)
			{
				Person? person = await _personRepository.GetAsync(personId);
				var @event = new PersonHasBeenInvitedToBbq(churras.Id, churras.Date, churras.Reason);
				person.Apply(@event);

				await _personRepository.SaveAsync(person);
			}

			return churras;
		}
	}
}
