# Test cover 
 
## cache.dll -                       87,73% 
	
## CacheEntry<T> -                   100%
## FileCacheStorage - 	             87,50%
## FileCacheStorage.FileCacheEntry - 100%

# Bug found

A bug was found. If directory exists on the disk and stored file exists, but it is corrupted and can't be deserialize,
when delete an entry from the cache the entryis not deleted.

The test case who show this bug is put to "#region failed test" 
Test name "Dir_Exist_File_Exist_And_Corrupted_Store_And_Delete_Entry"
    