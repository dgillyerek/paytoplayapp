# Workflow waiting to go live

`android-build.yml` here is the finished GitHub Actions workflow for Android builds (signed Play Store `.aab`, plus the
phone-install `.apk`). It lives here, not in `.github/workflows/`, only because the bot's GitHub login does not have
the `workflow` permission, and GitHub refuses any push that touches `.github/workflows/` without it.

To switch it on, either:

- **On GitHub (no tools needed):** open this file's *Raw* view, copy it, then *Add file → Create new file* on this
  branch with the name `.github/workflows/android-build.yml`, paste, and commit; or
- **Give the bot the permission:** approve the one-time GitHub code when the bot runs `gh auth refresh -s workflow`,
  and it moves the file itself.

The tests accept the workflow in either place and check the two copies match while both exist. Delete this folder
once the live copy is in.
