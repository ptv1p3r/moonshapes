using Moonshapes.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using System.Xml.Serialization;
using X.PagedList;
using System.Data.SqlClient;

namespace Moonshapes.Controllers
{
    public class UsersController : Controller
    {
        public const string IMAGE_FOLDER = "~/Images";
        public const string DATA_FOLDER = "~/App_Data";
        public const string XML_DATA_FILE = "data.xml";
        public const int PAGE_SIZE = 6;

        SqlConnection sqlConnection = new SqlConnection("Data Source=192.168.2.68,1433;Initial Catalog=moonshapes;User Id=sa;Password=Nadlor2902;");
        SqlCommand sqlCommand = null;
        SqlDataReader sqlDataReader = null;

        /// <summary>
        /// Metodo principal acao index
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            List<User> UserList = GetUserList();

            var std = UserList.OrderBy(p => p.Ordem).ToList();

            return View(std);
        }

        [HttpPost]
        public ActionResult Index(int[] UserId)
        {
            List<User> UserList = GetUserList();

            int ordem = 1;

            if (UserId != null)
            {
                foreach (int id in UserId)
                {
                    foreach (User user in UserList)
                    {
                        if (user.Id == id)
                        {
                            user.Ordem = ordem;
                            SetUser(user, true);
                        }
                    }
                    ordem += 1;
                }
            }

            return View(UserList.OrderBy(p => p.Ordem).ToList());
        }

        /// <summary>
        /// Metodo create novo utilizador
        /// </summary>
        /// <returns></returns>
        public ActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Metodo de post de validacao de email
        /// </summary>
        /// <param name="Email"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult doesEmailExist(string Email, int Id)
        {

            bool emailExists = GetEmail(Email, Id);

            return Json(!emailExists);
        }

        /// <summary>
        /// Metodo de post de criacao de uttilizador
        /// </summary>
        /// <param name="newUser"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Create(User newUser)
        {
            if (ModelState.IsValid)
            {
                AddUser(newUser);

                return RedirectToAction("Index");
            }
            else
            {
                return View(newUser);
            }
        }

        /// <summary>
        /// Metodo de acao de eliminacao de utilizador
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public ActionResult Delete(int Id)
        {
            IList<User> UserList = GetUserList();

            var std = UserList.Where(s => s.Id == Id).FirstOrDefault();

            return View(std);
        }

        /// <summary>
        /// Metodo de post de eliminacao de utilizador
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            DeleteUser(id);

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Metodo de acao de detalhes de utilizador
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public ActionResult Details(int Id)
        {
            IList<User> UserList = GetUserList();

            var std = UserList.Where(s => s.Id == Id).FirstOrDefault();

            return View(std);
        }

        /// <summary>
        /// Metodo de acao de edicao de utilizador
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public ActionResult Edit(int Id)
        {
            IList<User> UserList = GetUserList();

            var std = UserList.Where(s => s.Id == Id).FirstOrDefault();

            return View(std);
        }

        /// <summary>
        /// Metodod de post de edicao de utilizador
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Edit(User user)
        {
            SetUser(user);

            return RedirectToAction("Index");
        }

        public ActionResult List(int pagina = 1)
        {
            IList<User> UserList = GetUserList();

            var std = UserList.OrderBy(x => x.Id).ToPagedList(pagina, PAGE_SIZE);

            return View(std);
        }

        /// <summary>
        /// Metodo que efetua a exportação de dados para xml
        /// </summary>
        /// <returns></returns>
        public ActionResult ExportXml()
        {
            string response = string.Empty;

            try
            {
                List<User> UserList = GetUserList();

                // existe dados para exportacao
                if (UserList.Count > 0)
                {                    
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<User>));

                    using (FileStream fileStream = System.IO.File.Create(Path.Combine(Server.MapPath(DATA_FOLDER), XML_DATA_FILE)))
                    {
                        xmlSerializer.Serialize(fileStream, UserList);
                    }

                }
                response = "Foram exportados com sucesso [" + UserList.Count + "] registos!";
            }
            catch (Exception)
            {
                response = "Erro na exportação!";
            }

            return Content("<script language='javascript' type='text/javascript'>alert('" + response + "');window.location = '/Users/Index';</script>");
        }

        /// <summary>
        /// Metodo que efetua a importacao de dados para bd de ficheiro xml
        /// </summary>
        /// <returns></returns>
        public ActionResult ImportXml()
        {
            string sqlQuery = string.Empty;
            string response = string.Empty;

            try
            {
                List<User> userList = new List<User>();

                // existe ficheiro xml
                if (System.IO.File.Exists(Path.Combine(Server.MapPath(DATA_FOLDER), XML_DATA_FILE)))
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<User>));

                    using (FileStream fileStream = System.IO.File.OpenRead(Path.Combine(Server.MapPath(DATA_FOLDER), XML_DATA_FILE)))
                    {
                        userList = (List<User>)xmlSerializer.Deserialize(fileStream);
                    }
                }
                              
                // tem dados para importacao
                if (userList.Count > 0)
                {
                    sqlConnection.Open();

                    // limpa bd
                    sqlQuery = "DELETE FROM users";
                    using (sqlCommand = new SqlCommand(sqlQuery, sqlConnection))
                    {
                        sqlCommand.ExecuteNonQuery();
                    }

                    // insere novos dados
                    foreach (User user in userList)
                    {
                        sqlQuery = "SET IDENTITY_INSERT users ON;INSERT INTO users (id, nome, email, dnascimento, foto, ordem) VALUES (" +
                                user.Id +", '" + user.Name + "','" + user.Email + "','" +
                                user.DataNascimento.ToString("yyyy-MM-dd") + "','" + user.Foto + "', " + user.Ordem + ")";

                        using (sqlCommand = new SqlCommand(sqlQuery, sqlConnection))
                        {
                            sqlCommand.ExecuteNonQuery();
                        }
                    }
                }

                response = "Foram importados com sucesso [" + userList.Count + "] registos!";
            }
            catch (Exception)
            {
                response = "Erro na importação!";
            }
            finally
            {
                if (sqlConnection != null)
                {
                    sqlConnection.Close();
                }
            }

            return Content("<script language='javascript' type='text/javascript'>alert('" + response + "');window.location = '/Users/Index';</script>");
        }

        /// <summary>
        /// Metodo que retorna lista de utilizadores da base de dados
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        private List<User> GetUserList(int Id = 0)
        {
            List<User> UserList = new List<User>();
            string sqlQuery = string.Empty;

            try
            {
                sqlConnection.Open();

                if (Id == 0)
                {
                    sqlQuery = "SELECT * FROM users";
                }
                else
                {
                    sqlQuery = "SELECT * FROM users WHERE id = " + Id;
                }
                
                using (sqlCommand = new SqlCommand(sqlQuery, sqlConnection))
                {
                    using (sqlDataReader = sqlCommand.ExecuteReader())
                    {
                        while (sqlDataReader.Read())
                        {
                            UserList.Add(new User()
                            {
                                Id = Convert.ToInt32(sqlDataReader["id"]),
                                Name = sqlDataReader["nome"].ToString(),
                                Email = sqlDataReader["email"].ToString(),
                                DataNascimento = Convert.ToDateTime(sqlDataReader["dnascimento"]),
                                Foto = sqlDataReader["foto"].ToString(),
                                Ordem = Convert.ToInt16(sqlDataReader["ordem"])
                            });
                        }
                    }
                }

            }
            catch (Exception e)
            {

            }
            finally
            {
                sqlConnection.Close();
            }

            return UserList;
        }

        /// <summary>
        /// Metodo que adiciona utilizador a base de dados
        /// </summary>
        /// <param name="_User"></param>
        private void AddUser(User user)
        {
            string sqlQuery = string.Empty;

            try
            {
                sqlConnection.Open();

                // Retorna ordem
                sqlQuery = @"SELECT MAX(ordem)+1 from users";
                using (sqlCommand = new SqlCommand(sqlQuery, sqlConnection))
                {
                    user.Ordem = Convert.ToInt32(sqlCommand.ExecuteScalar() == DBNull.Value ? 1 : sqlCommand.ExecuteScalar());
                }

                // insere user
                sqlQuery = "INSERT INTO users (nome, email, dnascimento, ordem) VALUES ('" +
                    user.Name + "','" + user.Email + "','" +
                    user.DataNascimento.ToString("yyyy-MM-dd") + "', " + user.Ordem +")";

                using (sqlCommand = new SqlCommand(sqlQuery, sqlConnection))
                {
                    sqlCommand.ExecuteNonQuery();
                }

                // Retorna id criado
                sqlQuery = @"SELECT SCOPE_IDENTITY()";
                using (sqlCommand = new SqlCommand(sqlQuery, sqlConnection))
                {
                    user.Id = Convert.ToInt32(sqlCommand.ExecuteScalar());
                }

                SaveImage(user); // grava imagem

                // update do caminho da imagem criada
                sqlQuery = @"UPDATE users SET foto = '" + user.Foto +"' WHERE id = " + user.Id;
                using (sqlCommand = new SqlCommand(sqlQuery, sqlConnection))
                {
                    sqlCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Response.Write("<script language='javascript'>alert('" + ex.Message + "')</script>");
            }
            finally
            {
                sqlConnection.Close();
            }
        }

        /// <summary>
        /// Metodo que edita utilizador na base de dados
        /// </summary>
        /// <param name="_User"></param>
        private void SetUser(User _User, bool orderOnly = false)
        {
            try
            {
                string sqlQuery = string.Empty;

                if (!orderOnly)
                {
                    DeleteImage(_User); // elimina imagem

                    SaveImage(_User); // grava nova imagem

                    // update user
                    sqlQuery = "UPDATE users SET nome = '" + _User.Name + "'," +
                        "email = '" + _User.Email + "'," +
                        "dnascimento = '" + _User.DataNascimento.ToString("yyyy-MM-dd") + "'," +
                        "foto = '" + _User.Foto + "', " +
                        "ordem = " + _User.Ordem + " " +
                        "WHERE id =" + _User.Id;

                } else
                {
                    // update user order
                    sqlQuery = "UPDATE users SET ordem = " + _User.Ordem + " " +
                        "WHERE id =" + _User.Id;
                }
                
                sqlConnection.Open();

                using (sqlCommand = new SqlCommand(sqlQuery, sqlConnection))
                {
                    sqlCommand.ExecuteNonQuery();
                }

            }
            catch ( Exception ex)
            {
                Response.Write("<script language='javascript'>alert('" + ex.Message + "')</script>");
            }
            finally
            {
                sqlConnection.Close();
            }
        }

        /// <summary>
        /// Metodo que elimina utilizador da base de dados
        /// </summary>
        /// <param name="Id"></param>
        private void DeleteUser(int Id)
        {
            try
            {

                DeleteImage(null, true, Id); // elimina imagem

                sqlConnection.Open();

                // elimina user
                string sqlQuery = "DELETE FROM users WHERE id =" + Id;
                using (sqlCommand = new SqlCommand(sqlQuery, sqlConnection))
                {
                    sqlCommand.ExecuteNonQuery();
                }

            }
            catch (Exception ex)
            {
                Response.Write("<script language='javascript'>alert('" + ex.Message + "')</script>");
            }
            finally
            {
                sqlConnection.Close();
            }
        }

        /// <summary>
        /// Metodo que valida a existencia de email na base de dados
        /// </summary>
        /// <param name="Email"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool GetEmail(string Email, int id)
        {
            bool exists = false;

            try
            {
                sqlConnection.Open();

                string sqlQuery = "SELECT * FROM users WHERE email = '" + Email + "' AND id <> " + id;
                using (sqlCommand = new SqlCommand(sqlQuery, sqlConnection))
                {
                    using (sqlDataReader = sqlCommand.ExecuteReader())
                    {
                        if (sqlDataReader.HasRows)
                        {
                            exists = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Response.Write("<script language='javascript'>alert('" + ex.Message + "')</script>");
            }
            finally
            {
                sqlConnection.Close();
            }
   
          return exists;
        }

        /// <summary>
        /// Metodo que grava imagem no hdd
        /// </summary>
        /// <param name="user"></param>
        private void SaveImage(User user)
        {
            try
            {
                string imageName = Path.GetFileName(user.File.FileName.Trim().ToLower());
                string path = Path.Combine(Server.MapPath(IMAGE_FOLDER), imageName);

                user.File.SaveAs(path);

                string imageHash = GetMD5Hash(path);
                imageHash = user.Id.ToString() + "_" + imageHash + ".jpg";
                string newPath = Path.Combine(Server.MapPath(IMAGE_FOLDER), imageHash);

                System.IO.File.Copy(path, newPath);
                System.IO.File.Delete(path);

                user.Foto = newPath;
                
            }
            catch (Exception ex)
            {
                Response.Write("<script language='javascript'>alert('" + ex.Message + "')</script>");
            }
        }

        /// <summary>
        /// Metodo que elimina imagens por utilizador ou por id
        /// </summary>
        /// <param name="user"></param>
        /// <param name="byId"></param>
        /// <param name="Id"></param>
        private void DeleteImage(User user = null, bool byId = false, int Id = 0)
        {
            try
            {
                if (!byId)
                {
                    if ((System.IO.File.Exists(user.Foto)))
                    {
                        System.IO.File.Delete(user.Foto);
                    }

                } else
                {
                    string filesToDelete = Id.ToString() + "_*.*";
                    string[] fileList = Directory.GetFiles(Server.MapPath(IMAGE_FOLDER), filesToDelete);
                    foreach (string file in fileList)
                    {
                        //System.Diagnostics.Debug.WriteLine(file + " a eliminar");
                        System.IO.File.Delete(file);
                    }
                }

            }
            catch (Exception ex)
            {
                Response.Write("<script language='javascript'>alert('" + ex.Message + "')</script>");
            }
        }

        /// <summary>
        /// Metodo que calcula um md5 de uma imagem
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private static string GetMD5Hash(string filePath)
        {
            byte[] computedHash = new MD5CryptoServiceProvider().ComputeHash(System.IO.File.ReadAllBytes(filePath));
            var sBuilder = new StringBuilder();
            foreach (byte b in computedHash)
            {
                sBuilder.Append(b.ToString("x2").ToLower());
            }
            return sBuilder.ToString();
        }
    }
}