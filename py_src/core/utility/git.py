import subprocess

class Git:

    @staticmethod
    def GetHash():
        """Retrieve the current Git commit hash."""
        try:
            git_hash = subprocess.check_output(['git', 'rev-parse', 'HEAD']).strip().decode('utf-8')
            return git_hash
        except subprocess.CalledProcessError:
            return "No git hash found"

    @staticmethod
    def ListFiles():
        """List all staged and unstaged files in the Git repository."""
        try:
            status_output = subprocess.check_output(['git', 'status', '--porcelain']).decode('utf-8')
            files = status_output.splitlines()
            staged_files = [file[3:] for file in files if file.startswith('A ') or file.startswith('M ')]
            unstaged_files = [file[3:] for file in files if file.startswith(' M') or file.startswith(' D')]
            return staged_files, unstaged_files
        except subprocess.CalledProcessError:
            return "Error retrieving file status"

    @staticmethod
    def GetHashAndMessage():
        """Retrieve the current Git commit hash and commit message."""
        try:
            # Get the latest commit hash and message
            commit_info = subprocess.check_output(['git', 'log', '-1', '--pretty=format:%H %s']).strip().decode('utf-8')
            return commit_info
        except subprocess.CalledProcessError:
            return "No git hash found"

