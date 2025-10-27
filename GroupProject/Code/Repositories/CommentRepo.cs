using GroupProject.Code.Models;
using Lab3GroupProject.Code.Data;
using Microsoft.EntityFrameworkCore;

namespace Lab3GroupProject.Repositories
{
    public class CommentRepo
    {
        private readonly ApplicationDbContext _context;
        
        public CommentRepo(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Comment> GetAllComments()
        {
            return _context.Comments.AsNoTracking().ToList();
        }

        public List<Comment> GetCommentsByEpisodeId(int episodeId)
        {
            return _context.Comments
                .AsNoTracking()
                .Where(c => c.EpisodeID == episodeId)
                .OrderByDescending(c => c.CommentDate)
                .ToList();
        }

        public Comment? GetCommentById(int commentId)
        {
            return _context.Comments.AsNoTracking().FirstOrDefault(c => c.CommentID == commentId);
        }

        public void AddComment(Comment comment)
        {
            _context.Comments.Add(comment);
            _context.SaveChanges();
        }

        public void UpdateComment(Comment comment)
        {
            // Detach any existing tracked entity
            var existingEntry = _context.ChangeTracker.Entries<Comment>()
                .FirstOrDefault(e => e.Entity.CommentID == comment.CommentID);
            
            if (existingEntry != null)
            {
                existingEntry.State = EntityState.Detached;
            }
            
            _context.Comments.Update(comment);
            _context.SaveChanges();
        }

        public void DeleteComment(int commentId)
        {
            var comment = _context.Comments.Find(commentId);
            if (comment != null)
            {
                _context.Comments.Remove(comment);
                _context.SaveChanges();
            }
        }

        public List<int> GetEpisodeIdsWithComments()
        {
            return _context.Comments
                .AsNoTracking()
                .Select(c => c.EpisodeID)
                .Distinct()
                .ToList();
        }
    }
}