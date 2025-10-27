using Amazon.DynamoDBv2.Model;
using GroupProject.Code.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lab3GroupProject.Code.Data;

namespace Lab3GroupProject.Repositories
{
    public class SubscriptionRepo
    {
        private readonly ApplicationDbContext _dbContext;

        public SubscriptionRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public IEnumerable<Subscription> GetAllSubscriptions()
        {
            return _dbContext.Subscriptions.ToList();
        }
        public Subscription? GetSubscriptionById(int id)
        {
            return _dbContext.Subscriptions.Find(id);
        }
        public void AddSubscription(Subscription subscription)
        {
            _dbContext.Subscriptions.Add(subscription);
            _dbContext.SaveChanges();
        }
        public void Update(Subscription subscription)
        {
            _dbContext.Subscriptions.Update(subscription);
            _dbContext.SaveChanges();
        }
        public void Delete(int id)
        {
            var subscription = _dbContext.Subscriptions.Find(id);
            if (subscription != null)
            {
                _dbContext.Subscriptions.Remove(subscription);
                _dbContext.SaveChanges();
            }
        }
    }
}