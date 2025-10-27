using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lab3GroupProject.Repositories;
using GroupProject.Code.Models;

namespace Lab3GroupProject.Service
{
    public class SubscriptionService
    {
        private readonly SubscriptionRepo _subRepository;
        public SubscriptionService(SubscriptionRepo subscriptionRepository)
        {
            _subRepository = subscriptionRepository;
        }
        public IEnumerable<Subscription> GetAllSubscriptions()
        {
            return _subRepository.GetAllSubscriptions();
        }
        public Subscription? GetSubscriptionById(int id)
        {
            return _subRepository.GetSubscriptionById(id);
        }
        public void CreateSubscription(Subscription subscription)
        {
            _subRepository.AddSubscription(subscription);
        }
        public void UpdateSubscription(Subscription subscription)
        {
            _subRepository.Update(subscription);
        }
        public void DeleteSubscription(int id)
        {
            _subRepository.Delete(id);
        }
    }
}