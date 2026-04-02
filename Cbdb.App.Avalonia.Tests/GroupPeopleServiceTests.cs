using Cbdb.App.Core;
using Cbdb.App.Data;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Cbdb.App.Avalonia.Tests;

public sealed class GroupPeopleServiceTests {
    [Fact]
    public async Task QueryAsync_LimitsResultsToRequestedPeopleAndRespectsAddressMode() {
        var sqlitePath = await CreateGroupPeopleDatabaseAsync();

        try {
            var service = new SqliteGroupPeopleService();

            var allAddressResult = await service.QueryAsync(
                sqlitePath,
                new[] { 2, 1, 2 },
                new GroupPeopleQueryOptions(true, true, true, true, true, GroupPeopleAddressMode.AllAddresses)
            );

            Assert.Equal(new[] { 1, 2 }, allAddressResult.StatusRecords.Select(row => row.PersonId).Distinct().OrderBy(id => id).ToArray());
            Assert.Equal(new[] { 1, 2 }, allAddressResult.OfficeRecords.Select(row => row.PersonId).Distinct().OrderBy(id => id).ToArray());
            Assert.Equal(new[] { 1, 2 }, allAddressResult.EntryRecords.Select(row => row.PersonId).Distinct().OrderBy(id => id).ToArray());
            Assert.Equal(new[] { 1, 2 }, allAddressResult.TextRecords.Select(row => row.PersonId).Distinct().OrderBy(id => id).ToArray());
            Assert.DoesNotContain(allAddressResult.StatusRecords, row => row.PersonId == 3);
            Assert.Contains(allAddressResult.AddressRecords, row => row.PersonId == 1 && row.IsIndexAddress);
            Assert.Contains(allAddressResult.AddressRecords, row => row.PersonId == 1 && !row.IsIndexAddress);

            var indexAddressResult = await service.QueryAsync(
                sqlitePath,
                new[] { 1, 2 },
                new GroupPeopleQueryOptions(false, false, false, false, true, GroupPeopleAddressMode.IndexAddresses)
            );

            Assert.All(indexAddressResult.AddressRecords, row => Assert.True(row.IsIndexAddress));
            Assert.Equal(2, indexAddressResult.AddressRecords.Count);
        } finally {
            TestSqliteFileHelper.Delete(sqlitePath);
        }
    }

    private static async Task<string> CreateGroupPeopleDatabaseAsync() {
        var path = Path.Combine(Path.GetTempPath(), $"cbdb-group-people-{Guid.NewGuid():N}.sqlite3");

        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
CREATE TABLE BIOG_MAIN (
    c_personid INTEGER PRIMARY KEY,
    c_name_chn TEXT,
    c_name TEXT,
    c_index_addr_id INTEGER,
    c_index_addr_type_code INTEGER,
    c_index_year INTEGER
);

CREATE TABLE STATUS_DATA (
    c_personid INTEGER,
    c_sequence INTEGER,
    c_status_code INTEGER,
    c_firstyear INTEGER,
    c_lastyear INTEGER,
    c_source INTEGER,
    c_pages TEXT,
    c_notes TEXT
);

CREATE TABLE STATUS_CODES (
    c_status_code INTEGER,
    c_status_desc_chn TEXT,
    c_status_desc TEXT
);

CREATE TABLE POSTED_TO_OFFICE_DATA (
    c_personid INTEGER,
    c_posting_id INTEGER,
    c_office_id INTEGER,
    c_sequence INTEGER,
    c_appt_type_code INTEGER,
    c_assume_office_code INTEGER,
    c_firstyear INTEGER,
    c_lastyear INTEGER,
    c_source INTEGER,
    c_pages TEXT,
    c_notes TEXT
);

CREATE TABLE OFFICE_CODES (
    c_office_id INTEGER,
    c_office_chn TEXT,
    c_office_pinyin TEXT,
    c_office_trans TEXT
);

CREATE TABLE APPOINTMENT_CODES (
    c_appt_code INTEGER,
    c_appt_desc_chn TEXT,
    c_appt_desc TEXT
);

CREATE TABLE ASSUME_OFFICE_CODES (
    c_assume_office_code INTEGER,
    c_assume_office_desc_chn TEXT,
    c_assume_office_desc TEXT
);

CREATE TABLE POSTED_TO_ADDR_DATA (
    c_personid INTEGER,
    c_posting_id INTEGER,
    c_office_id INTEGER,
    c_addr_id INTEGER
);

CREATE TABLE ENTRY_DATA (
    c_personid INTEGER,
    c_sequence INTEGER,
    c_entry_code INTEGER,
    c_year INTEGER,
    c_entry_addr_id INTEGER,
    c_source INTEGER,
    c_pages TEXT,
    c_notes TEXT
);

CREATE TABLE ENTRY_CODES (
    c_entry_code INTEGER,
    c_entry_desc_chn TEXT,
    c_entry_desc TEXT
);

CREATE TABLE BIOG_TEXT_DATA (
    c_personid INTEGER,
    c_textid INTEGER,
    c_role_id INTEGER,
    c_year INTEGER,
    c_source INTEGER,
    c_pages TEXT,
    c_notes TEXT
);

CREATE TABLE TEXT_CODES (
    c_textid INTEGER,
    c_title_chn TEXT,
    c_title TEXT
);

CREATE TABLE TEXT_ROLE_CODES (
    c_role_id INTEGER,
    c_role_desc_chn TEXT,
    c_role_desc TEXT
);

CREATE TABLE BIOG_ADDR_DATA (
    c_personid INTEGER,
    c_addr_id INTEGER,
    c_addr_type INTEGER,
    c_firstyear INTEGER,
    c_lastyear INTEGER,
    c_source INTEGER,
    c_notes TEXT
);

CREATE TABLE BIOG_ADDR_CODES (
    c_addr_type INTEGER,
    c_addr_desc_chn TEXT,
    c_addr_desc TEXT
);

CREATE TABLE ADDR_CODES (
    c_addr_id INTEGER,
    c_name_chn TEXT,
    c_name TEXT
);

INSERT INTO BIOG_MAIN (c_personid, c_name_chn, c_name, c_index_addr_id, c_index_addr_type_code, c_index_year) VALUES
(1, '甲', 'Jia', 10, 1, 1001),
(2, '乙', 'Yi', 20, 1, 1002),
(3, '丙', 'Bing', 30, 1, 1003);

INSERT INTO STATUS_CODES (c_status_code, c_status_desc_chn, c_status_desc) VALUES
(11, '官', 'Official'),
(22, '士', 'Scholar');

INSERT INTO STATUS_DATA (c_personid, c_sequence, c_status_code, c_firstyear, c_lastyear, c_source, c_pages, c_notes) VALUES
(1, 1, 11, 1000, 1005, 900, '1-2', 'status one'),
(2, 1, 22, 1010, 1012, 900, '3-4', 'status two'),
(3, 1, 22, 1020, 1022, 900, '5-6', 'status three');

INSERT INTO OFFICE_CODES (c_office_id, c_office_chn, c_office_pinyin, c_office_trans) VALUES
(101, '知州', 'Zhizhou', 'Prefect'),
(202, '知縣', 'Zhixian', 'Magistrate');

INSERT INTO APPOINTMENT_CODES (c_appt_code, c_appt_desc_chn, c_appt_desc) VALUES
(1, '任命', 'Appointment');

INSERT INTO ASSUME_OFFICE_CODES (c_assume_office_code, c_assume_office_desc_chn, c_assume_office_desc) VALUES
(2, '到任', 'Assume Office');

INSERT INTO POSTED_TO_OFFICE_DATA (c_personid, c_posting_id, c_office_id, c_sequence, c_appt_type_code, c_assume_office_code, c_firstyear, c_lastyear, c_source, c_pages, c_notes) VALUES
(1, 5001, 101, 1, 1, 2, 1000, 1004, 900, '10', 'office one'),
(2, 5002, 202, 1, 1, 2, 1011, 1015, 900, '11', 'office two'),
(3, 5003, 202, 1, 1, 2, 1021, 1025, 900, '12', 'office three');

INSERT INTO POSTED_TO_ADDR_DATA (c_personid, c_posting_id, c_office_id, c_addr_id) VALUES
(1, 5001, 101, 11),
(2, 5002, 202, 21),
(3, 5003, 202, 31);

INSERT INTO ENTRY_CODES (c_entry_code, c_entry_desc_chn, c_entry_desc) VALUES
(301, '進士', 'Jinshi'),
(302, '蔭補', 'Privilege');

INSERT INTO ENTRY_DATA (c_personid, c_sequence, c_entry_code, c_year, c_entry_addr_id, c_source, c_pages, c_notes) VALUES
(1, 1, 301, 1001, 12, 900, '20', 'entry one'),
(2, 1, 302, 1008, 22, 900, '21', 'entry two'),
(3, 1, 302, 1018, 32, 900, '22', 'entry three');

INSERT INTO TEXT_CODES (c_textid, c_title_chn, c_title) VALUES
(401, '文集甲', 'Collected Works A'),
(402, '文集乙', 'Collected Works B'),
(900, '來源', 'Source');

INSERT INTO TEXT_ROLE_CODES (c_role_id, c_role_desc_chn, c_role_desc) VALUES
(1, '作者', 'Author');

INSERT INTO BIOG_TEXT_DATA (c_personid, c_textid, c_role_id, c_year, c_source, c_pages, c_notes) VALUES
(1, 401, 1, 1003, 900, '30', 'text one'),
(2, 402, 1, 1009, 900, '31', 'text two'),
(3, 402, 1, 1019, 900, '32', 'text three');

INSERT INTO BIOG_ADDR_CODES (c_addr_type, c_addr_desc_chn, c_addr_desc) VALUES
(1, '索引地址', 'Index Address'),
(2, '居住地', 'Residence');

INSERT INTO ADDR_CODES (c_addr_id, c_name_chn, c_name) VALUES
(10, '甲地', 'Place A'),
(11, '甲任所', 'Office A'),
(12, '甲入仕地', 'Entry A'),
(13, '甲居地', 'Residence A'),
(20, '乙地', 'Place B'),
(21, '乙任所', 'Office B'),
(22, '乙入仕地', 'Entry B'),
(23, '乙居地', 'Residence B'),
(30, '丙地', 'Place C'),
(31, '丙任所', 'Office C'),
(32, '丙入仕地', 'Entry C');

INSERT INTO BIOG_ADDR_DATA (c_personid, c_addr_id, c_addr_type, c_firstyear, c_lastyear, c_source, c_notes) VALUES
(1, 13, 2, 999, 1002, 900, 'addr one'),
(2, 23, 2, 1005, 1008, 900, 'addr two'),
(3, 31, 2, 1015, 1018, 900, 'addr three');
""";
        await command.ExecuteNonQueryAsync();

        return path;
    }
}
