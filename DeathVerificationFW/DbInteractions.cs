using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace DeathVerificationFW
{
    public class DbInteractions
    {
        public class Utils
        {
            public static string GetConn()
            {
                var settings = ConfigurationManager.ConnectionStrings["connlive"];
                return Convert.ToString(settings);
            }
        }

        public class CheckMethods
        {
            public static string RecordInUseBy(int recId, string currentUser)
            {
                string inUseBy;
                var cmdText = "SELECT InUseBy FROM tLegacyDeaths WHERE ID = @recID";

                using (var conn = new SqlConnection(DbInteractions.Utils.GetConn()))
                {
                    using (var cmd = new SqlCommand(cmdText, conn))
                    {
                        cmd.Parameters.Add("@recID", SqlDbType.BigInt).Value = recId;
                        conn.Open();
                        var result = cmd.ExecuteScalar().ToString();
                        inUseBy = result == string.Empty ? "" : result;
                        conn.Close();
                    }
                }

                return inUseBy == currentUser || string.IsNullOrEmpty(inUseBy) ? "" : inUseBy;
            }

            public static bool IsDead(string ssn, bool ld=false)
            {

                const string pdQuery = "SELECT DISTINCT SSN as records FROM PersonalData WHERE SSN = @VAL AND Died IS NULL;";
                const string ldQuery = "SELECT DISTINCT SSN as records FROM tLegacyDeaths WHERE SSN = @VAL AND (isDead IS NULL OR isDead = 0) AND hasMatch = 1;";

                var query = ld ? ldQuery : pdQuery;
                var dt = new DataTable();
                using (var conn = new SqlConnection(Utils.GetConn()))
                {
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@VAL", ssn);
                        cmd.Connection.Open();
                        using (var dr = cmd.ExecuteReader())
                        {
                            dt.Load(dr);
                            using (var da = new SqlDataAdapter(cmd))
                            {
                                da.Fill(dt);
                            }
                        }
                    }
                }

                foreach (DataRow dr in dt.Rows)
                {
                    if (dr.Field<string>("records").Length > 2)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public class AlterMethods
        {
            public static void SetDeathPd(string ssn, string dod)
            {
                using (var conn = new SqlConnection(Utils.GetConn()))
                {
                    using (var cmd = new SqlCommand("sp_ViatorDied", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@SSN", SqlDbType.VarChar).Value = ssn;
                        cmd.Parameters.Add("@Died", SqlDbType.DateTime).Value = dod;
                        cmd.Parameters.Add("@SSADeathMaster", SqlDbType.Bit).Value = 0;
                        cmd.Parameters.Add("@SSDI", SqlDbType.Bit).Value = 0;
                        cmd.Parameters.Add("@Website", SqlDbType.Bit).Value = 0;
                        cmd.Parameters.Add("@WebsiteComment", SqlDbType.VarChar).Value = " ";
                        cmd.Parameters.Add("@Emails", SqlDbType.Bit).Value = 0;
                        cmd.Parameters.Add("@EmailsComment", SqlDbType.VarChar).Value = " ";
                        cmd.Parameters.Add("@Other", SqlDbType.Bit).Value = 1;
                        cmd.Parameters.Add("@OtherComment", SqlDbType.VarChar).Value = $"LDS: {EnvMethods.GetCurrentUser()}";
                        cmd.Parameters.Add("@UsrName", SqlDbType.VarChar).Value = $"LDS: {EnvMethods.GetCurrentUser()}";
                        cmd.Parameters.Add("@DMFMatching", SqlDbType.Bit).Value = 0;
                        cmd.Parameters.Add("@SSADeathMasterComment", SqlDbType.VarChar).Value = " ";
                        cmd.Parameters.Add("@DODPriorDateCompleted", SqlDbType.Bit).Value = 0;
                        cmd.Parameters.Add("@Comment", SqlDbType.VarChar).Value = " ";
                        cmd.Parameters.Add("@Comserv", SqlDbType.Bit).Value = 0;

                        // Console.WriteLine(cmd.CommandText);

                        cmd.Connection.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

    }
}
