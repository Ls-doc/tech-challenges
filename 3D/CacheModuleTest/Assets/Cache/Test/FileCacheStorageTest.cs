using Zenject;
using NUnit.Framework;
using System;
using System.Text;
using System.IO;
using Moq;

/// <summary>
/// Hint: Keywords you may need: Mock, Setup, It.IsAny, Returns, Throws, Assert
/// </summary>
[TestFixture]
public class FileCacheStorageTest : ZenjectUnitTestFixture
{
    private Mock<IFileSystem> _fileSystemMock = new Mock<IFileSystem>();
    private Mock<ISerializer> _serializerMock = new Mock<ISerializer>();
    private string _curDir = AppDomain.CurrentDomain.BaseDirectory;
    private string _curFile = "testFile";
    private byte[] _data = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
    private byte[] _data2 = new byte[] { 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32 };

    // A sample test function to indicate the usage of the Test tag. 
    // https://docs.unity3d.com/2018.3/Documentation/Manual/testing-editortestsrunner.html
    [Test]
    public void SampleTest()
    {
    }

    [Test]
    public void Test_Empty_Storage_After_Creation()
    {
        // create storage
        FileCacheStorage storage = new FileCacheStorage(_curDir, _curFile, _fileSystemMock.Object, _serializerMock.Object);

        // check new storage: should not have FileCacheEntry and version
        Assert.AreEqual(false, storage.Has("1"));
        Assert.AreEqual(null, storage.Version);
        Assert.AreEqual(null, storage.Get("2"));
        Assert.AreEqual(0, storage.CacheEntries.Count);

        try
        {
            // should not have exception, if delete FileCacheEntry in empty storage
            storage.Delete("3");
        }
        catch (Exception ex)
        {
            Assert.Fail("No exception should be thrown in Delete() for empty storage, but got " + ex.Message);
        }

        // check storage version after saving storage to file
        storage.SaveCacheStorageFile();
        Assert.AreEqual("1.0", storage.Version);
    }

    [Test]
    public void Dir_Exist_File_Exist_And_Corrupted_StorageCreation_OK()
    {
        setupMocks(true);
        FileCacheStorage storage = new FileCacheStorage(_curDir, _curFile, _fileSystemMock.Object, _serializerMock.Object);
    }

    [Test]
    public void Store_And_verify_multiple_entries()
    {
        // create storage and fill it with 3 FileCacheEntry
        FileCacheStorage storage = createAndFillStorage(3);

        // verify all FileCacheEntry 
        Assert.AreEqual(true, storage.Has("1"));
        Assert.AreEqual(true, storage.Has("2"));
        Assert.AreEqual(true, storage.Has("3"));

        storage.SaveCacheStorageFile();
        Assert.AreEqual(3, storage.CacheEntries.Count);

        // current version have to be set after SaveCacheStorageFile
        Assert.AreNotEqual(null, storage.Version);
    }

    [Test]
    public void Store_And_delete_multiple_entry()
    {
        // create storage and fill it with 5 FileCacheEntry
        FileCacheStorage storage = createAndFillStorage(5);

        // verify all FileCacheEntry 
        Assert.AreEqual(true, storage.Has("1"));
        Assert.AreEqual(true, storage.Has("2"));
        Assert.AreEqual(true, storage.Has("3"));
        Assert.AreEqual(true, storage.Has("4"));
        Assert.AreEqual(true, storage.Has("5"));

        storage.SaveCacheStorageFile();
        // check FileCacheEntry count 
        Assert.AreEqual(5, storage.CacheEntries.Count);

        // delete FileCacheEntry with id = "2"
        storage.Delete("2");
        Assert.AreEqual(null, storage.Get("2"));
        Assert.AreNotEqual(null, storage.Get("3"));
        Assert.AreEqual(false, storage.Has("2"));


        storage.SaveCacheStorageFile();
        // check FileCacheEntry count 
        Assert.AreEqual(4, storage.CacheEntries.Count);

        // delete all and check
        storage.DeleteAll();
        storage.SaveCacheStorageFile();

        // check FileCacheEntry count 
        Assert.AreEqual(0, storage.CacheEntries.Count);
    }


    [Test]
    public void Store_Empty_Id()
    {
        FileCacheStorage storage = new FileCacheStorage(_curDir, _curFile, _fileSystemMock.Object, _serializerMock.Object);

        // test limits
        // store data with empty id
        storage.Set(_data, "");
        // store empty data with empty id
        storage.Set(new byte[0], "");
        storage.SaveCacheStorageFile();
        // check FileCacheEntry count 
        Assert.AreEqual(1, storage.CacheEntries.Count);
    }

    [Test]
    public void Test_Store_Same_entry_With_Versions()
    {
        // create storage
        FileCacheStorage storage = new FileCacheStorage(_curDir, _curFile, _fileSystemMock.Object, _serializerMock.Object);

        string id = "1";
        string version1 = "1.1";
        string version2 = "2.0";

        // store the data with id and version1
        storage.Set(_data, id, version1);

        // store the data with id and version2
        storage.Set(_data, id, version2);

        // check version of the FileCacheEntry with id in the storage
        Assert.AreEqual(true, storage.Has(id));
        Assert.AreEqual(true, storage.MatchesVersion(id, version2));
        Assert.AreEqual(false, storage.MatchesVersion(id, version1));

        string version3 = "3.0";
        // store the data with id and version3
        storage.Set(_data, id, version3);

        // check version of the FileCacheEntry with id in the storage
        Assert.AreEqual(true, storage.MatchesVersion(id, version3));
    }

    [Test]
    public void Test_CheckVersioning()
    {
        string version1 = "1.1";
        FileCacheStorage storage = createAndFillStorage(4, version1);

        string id = "1";
        string version2 = "2.0";
        // change version for id
        storage.Set(_data2, id, version2);
        // check if version changed
        Assert.AreEqual(true, storage.MatchesVersion(id, version2));
        // check old version 
        Assert.AreEqual(false, storage.MatchesVersion(id, version1));
    }

    private void setupMocks(bool fileExist = false)
    {
        _serializerMock.Setup(r => r.Serialize(It.IsAny<object>())).Returns("abcdefg");
        _serializerMock.Setup(r => r.Deserialize(It.IsAny<string>(), It.IsAny<object>()));

        _fileSystemMock.Setup(r => r.FileExists(It.IsAny<string>())).Returns(fileExist);
        _fileSystemMock.Setup(r => r.DirExists(It.IsAny<string>())).Returns(true);
        _fileSystemMock.Setup(r => r.Write(It.IsAny<string>(), _data, FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
        _fileSystemMock.Setup(r => r.Read(It.IsAny<string>(), FileMode.Open, FileAccess.Read, FileShare.Read)).Returns(new byte[] { 0x33, 0x33, 0x33, 0x33, 0x33 });
    }

    private FileCacheStorage createAndFillStorage(int entriesNumber, string version = "")
    {
        setupMocks();
        FileCacheStorage storage = new FileCacheStorage(_curDir, _curFile, _fileSystemMock.Object, _serializerMock.Object);

        for (int ii = 0; ii < entriesNumber; ii++)
        {
            string id = (ii + 1).ToString();
            storage.Set(_data, id, version);
        }
        return storage;
    }

    #region failed test 
    [Test]
    public void Dir_Exist_File_Exist_And_Corrupted_Store_And_Delete_Entry()
    {
        // define: directory - exist, file - exist, but corrupted
        setupMocks(true);
        FileCacheStorage storage = new FileCacheStorage(_curDir, _curFile, _fileSystemMock.Object, _serializerMock.Object);
        // store 2 FileCacheEntry
        string id1 = "1";
        string id2 = "2";
        storage.Set(_data, id1);
        storage.Set(_data, id2);
        // check FileCacheEntry numbers
        storage.SaveCacheStorageFile();
        Assert.AreEqual(2, storage.CacheEntries.Count);
        // delete "1" FileCacheEntry
        storage.Delete(id1);
        // check FileCacheEntry numbers
        storage.SaveCacheStorageFile();
        Assert.AreEqual(1, storage.CacheEntries.Count);
    }
    #endregion

}