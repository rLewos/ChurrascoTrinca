using Domain.Entities;
using Domain.Repositories;
using Eveneum;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Services
{
	public class BbqService : IBbqService
	{
		private readonly IBbqRepository _bbqRepository;
        public BbqService(IBbqRepository bbqRepository)
        {
			_bbqRepository = bbqRepository;
		}

        public async Task<Bbq?> GetAsync(string streamId)
		{
			return await _bbqRepository.GetAsync(streamId);
		}

		public async Task<StreamHeaderResponse> GetHeaderAsync(string streamId)
		{
			return await _bbqRepository.GetHeaderAsync(streamId);
		}

		public async Task SaveAsync(Bbq entity)
		{
			await _bbqRepository.SaveAsync(entity);
		}
	}
}
