using Cbdb.App.Data;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Cbdb.App.Avalonia.Tests;

public sealed class PersonBrowserServiceSchemaTests {
    [Fact]
    public async Task GetPostingsAsync_LoadsCurrentAppointmentColumn() {
        var sqlitePath = Path.Combine(Path.GetTempPath(), $"cbdb-person-postings-{Guid.NewGuid():N}.sqlite3");

        try {
            await using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder {
                DataSource = sqlitePath,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ConnectionString)) {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = """
CREATE TABLE POSTING_DATA (
    c_personid INTEGER,
    c_posting_id INTEGER
);
CREATE TABLE POSTED_TO_OFFICE_DATA (
    c_personid INTEGER,
    c_office_id INTEGER,
    c_posting_id INTEGER,
    c_sequence INTEGER,
    c_firstyear INTEGER,
    c_fy_nh_code INTEGER,
    c_fy_nh_year INTEGER,
    c_fy_range INTEGER,
    c_fy_month INTEGER,
    c_fy_intercalary INTEGER,
    c_fy_day INTEGER,
    c_fy_day_gz INTEGER,
    c_lastyear INTEGER,
    c_ly_nh_code INTEGER,
    c_ly_nh_year INTEGER,
    c_ly_range INTEGER,
    c_ly_month INTEGER,
    c_ly_intercalary INTEGER,
    c_ly_day INTEGER,
    c_ly_day_gz INTEGER,
    c_appt_code INTEGER,
    c_assume_office_code INTEGER,
    c_inst_code INTEGER,
    c_inst_name_code INTEGER,
    c_source INTEGER,
    c_pages TEXT,
    c_notes TEXT,
    c_office_category_id INTEGER,
    c_dy INTEGER,
    c_created_by TEXT,
    c_created_date TEXT,
    c_modified_by TEXT,
    c_modified_date TEXT
);
CREATE TABLE POSTED_TO_ADDR_DATA (
    c_posting_id INTEGER,
    c_office_id INTEGER,
    c_personid INTEGER,
    c_addr_id INTEGER,
    c_created_by TEXT,
    c_created_date TEXT,
    c_modified_by TEXT,
    c_modified_date TEXT
);
CREATE TABLE OFFICE_CODES (c_office_id INTEGER, c_office_chn TEXT, c_office_pinyin TEXT);
CREATE TABLE APPOINTMENT_CODES (c_appt_code INTEGER, c_appt_desc_chn TEXT, c_appt_desc TEXT);
CREATE TABLE ASSUME_OFFICE_CODES (c_assume_office_code INTEGER, c_assume_office_desc_chn TEXT, c_assume_office_desc TEXT);
CREATE TABLE OFFICE_CATEGORIES (c_office_category_id INTEGER, c_category_desc_chn TEXT, c_category_desc TEXT);
CREATE TABLE NIAN_HAO (c_nianhao_id INTEGER, c_nianhao_chn TEXT, c_nianhao_pin TEXT);
CREATE TABLE YEAR_RANGE_CODES (c_range_code INTEGER, c_range_chn TEXT, c_range TEXT);
CREATE TABLE GANZHI_CODES (c_ganzhi_code INTEGER, c_ganzhi_chn TEXT, c_ganzhi_py TEXT);
CREATE TABLE DYNASTIES (c_dy INTEGER, c_dynasty_chn TEXT, c_dynasty TEXT);
CREATE TABLE TEXT_CODES (c_textid INTEGER, c_title_chn TEXT, c_title TEXT);
CREATE TABLE ADDR_CODES (c_addr_id INTEGER, c_name_chn TEXT, c_name TEXT);

INSERT INTO POSTING_DATA VALUES (1, 5001);
INSERT INTO OFFICE_CODES VALUES (101, '知州', 'Zhizhou');
INSERT INTO APPOINTMENT_CODES VALUES (1, '任命', 'Appointment');
INSERT INTO ASSUME_OFFICE_CODES VALUES (2, '到任', 'Assume Office');
INSERT INTO OFFICE_CATEGORIES VALUES (3, '文職', 'Civil');
INSERT INTO DYNASTIES VALUES (4, '宋', 'Song');
INSERT INTO TEXT_CODES VALUES (900, '來源', 'Source');
INSERT INTO ADDR_CODES VALUES (10, '杭州', 'Hangzhou');
INSERT INTO POSTED_TO_OFFICE_DATA
    (c_personid, c_office_id, c_posting_id, c_sequence, c_firstyear, c_appt_code, c_assume_office_code, c_source, c_pages, c_notes, c_office_category_id, c_dy)
VALUES
    (1, 101, 5001, 1, 1000, 1, 2, 900, '10', 'note', 3, 4);
INSERT INTO POSTED_TO_ADDR_DATA
    (c_posting_id, c_office_id, c_personid, c_addr_id)
VALUES
    (5001, 101, 1, 10);
""";
                await command.ExecuteNonQueryAsync();
            }

            var service = new SqlitePersonBrowserService();
            var postings = await service.GetPostingsAsync(sqlitePath, 1);

            var posting = Assert.Single(postings);
            var office = Assert.Single(posting.Offices);
            Assert.Equal("任命 / Appointment", office.AppointmentType);
            Assert.Equal("知州", office.OfficeNameChn);
            Assert.Equal("Zhizhou", office.OfficeName);
            Assert.Equal("杭州", Assert.Single(office.Addresses).AddressNameChn);
        } finally {
            TestSqliteFileHelper.Delete(sqlitePath);
        }
    }
}
