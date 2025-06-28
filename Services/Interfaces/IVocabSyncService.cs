using System.Collections.Generic;
using System.Threading.Tasks;

public interface IVocabSyncService
{
    Task<string> SyncFromGoogleDriveAsync();
}
