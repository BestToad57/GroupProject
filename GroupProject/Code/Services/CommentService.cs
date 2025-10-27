using Lab3GroupProject.Repositories;
using GroupProject.Code.Models;

namespace Lab3GroupProject.Service
{
    public class CommentService
    {
        private readonly CommentRepo _commentRepository;
        
        public CommentService(CommentRepo commentRepository)
        {
            _commentRepository = commentRepository;
        }

        public List<Comment> GetAllComments()
        {
            return _commentRepository.GetAllComments();
        }

        public List<Comment> GetCommentsByEpisodeId(int episodeId)
        {
            return _commentRepository.GetCommentsByEpisodeId(episodeId);
        }

        public Comment? GetCommentById(int commentId)
        {
            return _commentRepository.GetCommentById(commentId);
        }

        public void AddComment(Comment comment)
        {
            _commentRepository.AddComment(comment);
        }

        public void UpdateComment(Comment comment)
        {
            _commentRepository.UpdateComment(comment);
        }

        public void DeleteComment(int commentId)
        {
            _commentRepository.DeleteComment(commentId);
        }

        public List<int> GetEpisodeIdsWithComments()
        {
            return _commentRepository.GetEpisodeIdsWithComments();
        }
    }
}