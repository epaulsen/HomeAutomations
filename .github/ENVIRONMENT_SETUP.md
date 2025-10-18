# GitHub Environment Setup

## PROD Environment Configuration

The Docker publishing workflow uses a GitHub Environment called `PROD` that requires manual approval before publishing images to DockerHub.

### Setting up the PROD Environment

To configure the environment protection rules:

1. Go to your repository on GitHub
2. Navigate to **Settings** → **Environments**
3. Click **New environment** (or select `PROD` if it already exists)
4. Name it: `PROD`
5. Under **Deployment protection rules**, enable:
   - **Required reviewers** - Add yourself (@epaulsen) as a required reviewer
6. Click **Save protection rules**

### How It Works

When the workflow runs (triggered by a push to the `main` branch):
1. The workflow will start and pause before executing the job
2. You'll receive a notification requesting approval
3. You must manually approve the deployment in the GitHub Actions UI
4. Only after approval will the Docker image be built and pushed to DockerHub

### Approving Deployments

To approve a pending deployment:
1. Go to the **Actions** tab in your repository
2. Click on the workflow run that's waiting for approval
3. Click **Review deployments**
4. Select the `PROD` environment
5. Click **Approve and deploy**

This ensures that no Docker images are published without your explicit consent.
