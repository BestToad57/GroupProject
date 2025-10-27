/*
<form asp-controller="Comment" asp-action="Add" method="post" class="mb-3">
    <input type="hidden" name="episodeId" value="@ViewBag.EpisodeID" />
    <input type="hidden" name="podcastId" value="@ViewBag.PodcastID" />

    <div class="mb-2">
        <textarea name="text" class="form-control" rows="3" placeholder="Write a Comment" required></textarea>
    </div>
	<button type="submit" class="btn btn-primary">Add Comment</button>
</form>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
*/