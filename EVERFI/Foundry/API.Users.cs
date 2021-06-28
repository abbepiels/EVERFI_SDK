﻿using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using EVERFI.Foundry.Classes;
using RestSharp.Authenticators;

namespace EVERFI.Foundry
{
    public partial class API
    {
        public RestRequest ConfigureRequest()
        {
            Console.WriteLine("Choose one:\n 1. Get user by email \n 2. Get user by ID \n 3. Add a user \n 4. Update user \n 5. Get all users");
            string input = Console.ReadLine();
            int val = Convert.ToInt32(input);
            Console.WriteLine(val);
           RestRequest request = new RestRequest();
            switch (val)
            {
                case 1: //by email
                    request.Resource ="{version}/admin/users/";
                    request.Method = Method.GET;
                    request.AddParameter("version", _ver, ParameterType.UrlSegment);
                    request.AddHeader("Content-Type", "application/json");
                    request.AddParameter("Authorization", _token.token_type + " " + _token.access_token, ParameterType.HttpHeader);
                    break;
                case 2: //by ID
                    request.Resource = "{version}/admin/users/{id}";
                    request.Method = Method.GET;
                    request.AddParameter("version", _ver, ParameterType.UrlSegment);
                    request.AddParameter("include", "category_labels", ParameterType.QueryString);
                    request.AddHeader("Content-Type", "application/json");
                    request.AddParameter("Authorization", _token.token_type + " " + _token.access_token, ParameterType.HttpHeader);

                    break;
                case 3: //add user
                    request = new RestRequest("{version}/admin/registration_sets", Method.POST);
                    request.AddParameter("version", _ver, ParameterType.UrlSegment);
                    request.AddParameter("Authorization", _token.token_type + " " + _token.access_token, ParameterType.HttpHeader);
                    break;
                case 4: //update user
                    request = new RestRequest("{version}/admin/registration_sets/{id}", Method.PATCH);
                    request.AddParameter("version", _ver, ParameterType.UrlSegment);
                    break;
                case 5: //all users
                    request = new RestRequest("/{version}/admin/users", Method.GET);
                    request.AddParameter("version", _ver, ParameterType.UrlSegment);
                    request.AddParameter("page[page]", currPage, ParameterType.QueryString);
                    request.AddParameter("include", "category_labels", ParameterType.QueryString);
                    request.AddParameter("page[per_page]", returnPerPage, ParameterType.QueryString);
                    request.AddHeader("Content-Type", "application/json");
                    request.AddParameter("Authorization", _token.token_type + " " + _token.access_token, ParameterType.HttpHeader);
                    break;
            }
            Console.WriteLine(request.Body);
            return request; 
          
        }
        public void responseModifer(IRestResponse response, int type)
        {
        
            HttpStatusCode statusCode = response.StatusCode;
            int numericCode = (int)statusCode;
            if (type == 1)
            {
                if (numericCode != 200)
                {
                    throw new FoundryException(response.ErrorMessage, numericCode, response.Content);
                }
            }
           else if(type == 2)
            {
                if (numericCode != 200)
                {
                    throw new FoundryException(response.ErrorMessage, numericCode, response.Content);
                }
            }
           
        }
       
        public User AddUser(User MyUser)
        {
            RestRequest request = ConfigureRequest();
            request.AddParameter("application/json", API.UserJson(MyUser), ParameterType.RequestBody);


            IRestResponse response = _client.Execute<User>(request);
            responseModifer(response, 2);

            UserDataJson userData = JsonConvert.DeserializeObject<UserDataJson>(response.Content);
            Console.WriteLine("User successfully added.");

            User user = userData.Data.UserAttributes;

            if (user.Location != null)
            {
                user.Location = GetLocationById(user.LocationId);
            }


            user.ConfigureUserData(userData.Data);

            return user;
        }

        public User UpdateUser(User MyUser)
        {
            RestRequest request = ConfigureRequest();

            request.AddParameter("id", MyUser.UserId, ParameterType.UrlSegment);
            request.AddParameter("application/json", API.UserJson(MyUser), ParameterType.RequestBody);
            _client.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(_token.access_token, _token.token_type);

            IRestResponse response = _client.Execute(request);
            responseModifer(response, 1);

            UserDataJson userData = JsonConvert.DeserializeObject<UserDataJson>(response.Content);

            Console.WriteLine("User successfully updated.");

            User user = userData.Data.UserAttributes;

            if (user.Location != null)
            {
                user.Location = GetLocationById(user.LocationId);
            }

            user.ConfigureUserData(userData.Data);

            return user;
        }

        public User GetUserById(string UserId)
        {
            RestRequest request = ConfigureRequest();
           
            request.AddParameter("id", UserId, ParameterType.UrlSegment);
            
            IRestResponse response = _client.Execute(request);
            responseModifer(response, 1);

            UserDataJson userData = JsonConvert.DeserializeObject<UserDataJson>(response.Content);
            UserDataIncludedList userDataIncluded = JsonConvert.DeserializeObject<UserDataIncludedList>(response.Content);


            User retrievedUser = userData.Data.UserAttributes;

            retrievedUser.ConfigureUserData(userData.Data);
            if (retrievedUser.Location != null)
            {
                retrievedUser.Location = GetLocationById(retrievedUser.LocationId);
            }

            foreach (var labelAttribute in userDataIncluded.IncludedList)
            {
                Label newLabel = new Label();
                newLabel.Name = labelAttribute.LabelsAttributes.LabelName;
                newLabel.CategoryName = labelAttribute.LabelsAttributes.CategoryLabelName;
                newLabel.Id = labelAttribute.LabelId;
                newLabel.CategoryId = labelAttribute.LabelsAttributes.CategoryID;
                retrievedUser.Labels.Add(newLabel);
            }

            Console.WriteLine("User Retrieved: " + retrievedUser.FirstName + " " + retrievedUser.LastName + "...");

            return retrievedUser;
        }

        public List<User> GetUserByEmail(string UserEmail)
        {
            Console.WriteLine("Getting User(s) with email: " + UserEmail + "...");


            RestRequest request = ConfigureRequest();
            request.AddParameter("filter[email]", UserEmail, ParameterType.QueryString);
            
            IRestResponse response = _client.Execute(request);
            responseModifer(response, 1);

            UserDataJsonList userData = JsonConvert.DeserializeObject<UserDataJsonList>(response.Content);
            List<User> users = new List<User>();

            foreach (UserData data in userData.Data)
            {
                User newUser = data.UserAttributes;
                newUser.ConfigureUserData(data);
                if (newUser.Location != null)
                {
                    newUser.Location = GetLocationById(newUser.LocationId);
                }
                users.Add(newUser);
                Console.WriteLine("User Retrieved: " + newUser.FirstName + " " + newUser.LastName + "...");
            }

            return users;
        }

        // TODO: Implement paging in this
        public List<User> GetUsersBySearch(Dictionary<SearchTerms, string> searchTerms)
        {
            RestRequest request = new RestRequest("{version}/admin/users/", Method.GET);

            request.Parameters.Clear();
            request.AddParameter("version", _ver, ParameterType.UrlSegment);
            foreach (SearchTerms term in searchTerms.Keys)
            {
                request.AddParameter("filter[" + GetDescription(term) + "]", searchTerms[term], ParameterType.QueryString);
            }
            request.AddHeader("Content-Type", "application/json");
            request.AddParameter("Authorization", _token.token_type + " " + _token.access_token, ParameterType.HttpHeader);

            IRestResponse response = _client.Execute(request);
            responseModifer(response, 1);

            UserDataJsonList userData = JsonConvert.DeserializeObject<UserDataJsonList>(response.Content);
            List<User> users = new List<User>();

            foreach (UserData data in userData.Data)
            {
                User newUser = data.UserAttributes;
                newUser.ConfigureUserData(data);
                if (newUser.Location != null)
                {
                    newUser.Location = GetLocationById(newUser.LocationId);
                }
                users.Add(newUser);
            }

            return users;
        }

        public List<User> GetUsers(int page)
        {
            Console.WriteLine("Getting " + returnPerPage + "users on page " + page.ToString() + "...");

            RestRequest request = ConfigureRequest();
           
            IRestResponse response = _client.Execute(request);
            responseModifer(response,1);

            UserDataJsonList userData = JsonConvert.DeserializeObject<UserDataJsonList>(response.Content);
            UserDataIncludedList userDataIncluded = JsonConvert.DeserializeObject<UserDataIncludedList>(response.Content);
            List<Label> allLabels = new List<Label>();
            foreach (var labelAttribute in userDataIncluded.IncludedList)
            {
                Label newLabel = new Label();
                newLabel.Name = labelAttribute.LabelsAttributes.LabelName;
                newLabel.CategoryName = labelAttribute.LabelsAttributes.CategoryLabelName;
                newLabel.Id = labelAttribute.LabelId;
                newLabel.CategoryId = labelAttribute.LabelsAttributes.CategoryID;
                allLabels.Add(newLabel);
             
            }

            List<User> users = new List<User>();

            foreach (UserData data in userData.Data)
            {
                User newUser = data.UserAttributes;

                newUser.ConfigureUserData(data);
                
                foreach (Label lab in allLabels)
                {
                    if (data.multipleRelationships.categoryLabels.RelationshipArray[0].LabelId == lab.Id)
                    {
                        newUser.Labels.Add(lab);
                    }
                }
               
              
                if (newUser.Location != null)
                {
                    newUser.Location = GetLocationById(newUser.LocationId);
                }
                users.Add(newUser);
            }

            return users;
        }

        public (List<User>, bool) GetUsers()
        {
            bool returnValue = true;

            RestRequest request = new RestRequest("/{version}/admin/users", Method.GET);

            request.Parameters.Clear();
            request.AddParameter("version", _ver, ParameterType.UrlSegment);
            request.AddParameter("page[page]", currPage, ParameterType.QueryString);
            request.AddParameter("page[per_page]", returnPerPage, ParameterType.QueryString);
            request.AddHeader("Content-Type", "application/json");
            request.AddParameter("Authorization", _token.token_type + " " + _token.access_token, ParameterType.HttpHeader);

            IRestResponse response = _client.Execute(request);
            HttpStatusCode statusCode = response.StatusCode;
            int numericCode = (int)statusCode;

            if (numericCode != 200)
            {
                throw new FoundryException(response.ErrorMessage, numericCode, response.Content);
            }

            UserDataJsonList userData = JsonConvert.DeserializeObject<UserDataJsonList>(response.Content);
            List<User> users = new List<User>();

            foreach (UserData data in userData.Data)
            {
                User newUser = data.UserAttributes;
                newUser.ConfigureUserData(data);
                if (newUser.Location != null)
                {
                    newUser.Location = GetLocationById(newUser.LocationId);
                }
                users.Add(newUser);
            }

            MetaJson metaData = JsonConvert.DeserializeObject<MetaJson>(response.Content);

            Console.WriteLine("Returning " + users.Count + " users. Page " + currPage + " of " + Math.Ceiling((double)metaData.Meta.Count / returnPerPage));

            if (currPage * returnPerPage >= metaData.Meta.Count)
            {
                returnValue = false;
                currPage = 1;
            }
            else
            {
                currPage += 1;
            }

            return (users, returnValue);
        }

        public void ResetGetUsers()
        {
            currPage = 1;
        }

        public int GetUserCount()
        {
            RestRequest request = new RestRequest("/{version}/admin/users/?page[page]={page_num}&page[per_page]={num_per}", Method.GET);
            request.Parameters.Clear();
            request.AddParameter("version", _ver, ParameterType.UrlSegment);
            request.AddParameter("page_num", 1, ParameterType.UrlSegment);
            request.AddParameter("num_per", returnPerPage, ParameterType.UrlSegment);
            request.AddHeader("Content-Type", "application/json");
            request.AddParameter("Authorization", _token.token_type + " " + _token.access_token, ParameterType.HttpHeader);

            IRestResponse response = _client.Execute(request);
            HttpStatusCode statusCode = response.StatusCode;
            int numericCode = (int)statusCode;

            if (numericCode != 200)
            {
                throw new FoundryException(response.ErrorMessage, numericCode, response.Content);
            }

            MetaJson metaData = JsonConvert.DeserializeObject<MetaJson>(response.Content);

            return metaData.Meta.Count;
        }




        public List<Label> getLabels()
        {

            Console.WriteLine("Getting labels...");

            RestRequest request = new RestRequest("/{version}/admin/categories", Method.GET); //TODO
            request.AddParameter("version", _ver, ParameterType.UrlSegment);
            request.AddParameter("include", "category_labels", ParameterType.QueryString);
            request.AddHeader("Content-Type", "application/json");
            request.AddParameter("Authorization", _token.token_type + " " + _token.access_token, ParameterType.HttpHeader);

            IRestResponse response = _client.Execute(request);
            HttpStatusCode statusCode = response.StatusCode;
            int numericCode = (int)statusCode;

            if (numericCode != 200)
            {
                throw new FoundryException(response.ErrorMessage, numericCode, response.Content);
            }

            CategoryListData categoryData = JsonConvert.DeserializeObject<CategoryListData>(response.Content);
            List<Label> allLabels = new List<Label>();

            foreach (Category category in categoryData.Data)
            {
                category.ConfigureCategory();

                for (int i = 0; i < category.Labels.Count; i++)
                {
                    category.Labels[i] = GetLabelById(category.Labels[i].Id);
                    allLabels.Add(category.Labels[i]);
                }
            }

            return allLabels;


        }





    }

}





